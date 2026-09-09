using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FundingMonitor.CurrentRates.Application;
using FundingMonitor.CurrentRates.Application.Collection;
using FundingMonitor.CurrentRates.Infrastructure;
using FundingMonitor.CurrentRates.Infrastructure.Outbox;
using FundingMonitor.DatabaseMigrator;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.HistoricalRates.Application;
using FundingMonitor.HistoricalRates.Infrastructure;
using FundingMonitor.HistoricalRates.Infrastructure.Messaging;
using FundingMonitor.HistoricalRates.Infrastructure.Persistence;
using FundingMonitor.Messaging;
using FundingMonitor.Messaging.Abstractions;
using FundingMonitor.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Events;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.Messaging;

// All persistence, publisher confirms, hosted consumers, retry publication and ACKs are real.
// The only substitutes are exchange data and a precise process-loss boundary in the crash tests.
public sealed class CurrentToHistoricalEndToEndTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:4.2-management")
        .WithUsername("guest").WithPassword("guest").WithPortBinding(15672, true).Build();
    private readonly ControlledMarketData _market = new();
    private IHost? _host;
    private HttpClient _management = null!;
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(Ct), _rabbit.StartAsync(Ct));
        _management = new HttpClient
        {
            BaseAddress = new Uri($"http://{_rabbit.Hostname}:{_rabbit.GetMappedPublicPort(15672)}")
        };
        _management.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("guest:guest")));
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync(CancellationToken.None);
            _host.Dispose();
        }
        _management?.Dispose();
        await _rabbit.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task HostedDelivery_CommitsHistoryAndInbox_AcksDuplicates_RetriesTransient_DeadLettersPermanent()
    {
        _market.Symbols = ["BTC-USDT", "ETH-USDT", "SOL-USDT"];
        _market.Failures["ETH-USDT"] = ErrorType.Timeout;
        _market.Failures["SOL-USDT"] = ErrorType.Validation;
        _host = await CreateHostAsync();
        await CollectAndAssertTransactionAsync(3);
        await _host.StartAsync(Ct);

        await EventuallyAsync(async () =>
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var current = scope.ServiceProvider.GetRequiredService<CurrentRatesDbContext>();
            var history = scope.ServiceProvider.GetRequiredService<HistoricalRatesDbContext>();
            return await current.OutboxMessages.CountAsync(row => row.ProcessedOnUtc != null, Ct) == 3
                && await history.InboxMessages.CountAsync(Ct) == 2
                && await history.HistoricalFundingRates.CountAsync(Ct) == 2;
        }, "three outbox confirmations and two atomic history+Inbox commits");

        var connection = await _host.Services.GetRequiredService<RabbitMqConnection>().GetAsync(Ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: Ct);
        var deadLetter = new TaskCompletionSource<(string Body, int Retries)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var deadConsumer = new AsyncEventingBasicConsumer(channel);
        deadConsumer.ReceivedAsync += async (_, delivery) =>
        {
            deadLetter.TrySetResult((Encoding.UTF8.GetString(delivery.Body.Span),
                Convert.ToInt32(delivery.BasicProperties.Headers!["x-retry-count"])));
            await channel.BasicAckAsync(delivery.DeliveryTag, false, Ct);
        };
        await channel.BasicConsumeAsync(RabbitMqTopology.DeadQueueName, false, "test-dlq-observer", false, false,
            null, deadConsumer, Ct);
        var dead = await deadLetter.Task.WaitAsync(TimeSpan.FromSeconds(20), Ct);
        Assert.Contains("SOL-USDT", dead.Body);
        Assert.Equal(0, dead.Retries);
        Assert.Equal(1, _market.Calls["BTC-USDT"]);
        Assert.Equal(2, _market.Calls["ETH-USDT"]);
        Assert.Equal(1, _market.Calls["SOL-USDT"]);

        // Republish the identical event through the real transport, then observe actual Inbox deduplication.
        long duplicates = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Name == "fundingmonitor.inbox.duplicates") meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => Interlocked.Add(ref duplicates, value));
        listener.Start();
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var rows = await scope.ServiceProvider.GetRequiredService<CurrentRatesDbContext>()
                .OutboxMessages.AsNoTracking().ToListAsync(Ct);
            var row = Assert.Single(rows, item => OutboxEventSerializer.Deserialize(item.Payload).Symbol.Value == "BTC-USDT");
            var message = OutboxEventSerializer.Deserialize(row.Payload);
            Assert.Equal(MessagePublishResult.Success,
                await _host.Services.GetRequiredService<IMessagePublisher>().PublishAsync(
                    RabbitMqTopology.ExchangeName, RabbitMqTopology.GetRoutingKey(message.Exchange),
                    new MessageEnvelope<FundingMonitor.CurrentRates.Contracts.FundingRateChangedIntegrationEvent>(
                        message.EventId, message.EventId, null, message.OccurredOnUtc, "funding-rate.changed", 1, message),
                    null, Ct));
        }
        await EventuallyAsync(() => Task.FromResult(Interlocked.Read(ref duplicates) >= 1), "duplicate consumed via Inbox");
        await AssertAcknowledgedAsync(5); // Three originals, one retry, one duplicate.
        await using var verification = _host.Services.CreateAsyncScope();
        var persisted = verification.ServiceProvider.GetRequiredService<HistoricalRatesDbContext>();
        Assert.Equal(2, await persisted.InboxMessages.CountAsync(Ct));
        var points = await persisted.HistoricalFundingRates.AsNoTracking().ToListAsync(Ct);
        Assert.All(points, point => Assert.Equal(0.0003m, point.FundingRate));
        Assert.DoesNotContain(points, point => point.NormalizedSymbol == "SOL-USDT");
        Assert.Equal(1, _market.Calls["BTC-USDT"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HostedPublisher_ProcessLostAroundConfirm_RecoversLeaseWithoutLosingOrDuplicatingHistory(bool afterConfirm)
    {
        var fault = new ConfirmWindow(afterConfirm);
        _host = await CreateHostAsync(fault);
        await CollectAndAssertTransactionAsync(1);
        await _host.StartAsync(Ct);
        await fault.Reached.Task.WaitAsync(TimeSpan.FromSeconds(20), Ct);
        int attemptsBeforeLoss;
        await using (var scope = _host.Services.CreateAsyncScope())
            attemptsBeforeLoss = (await scope.ServiceProvider.GetRequiredService<CurrentRatesDbContext>()
                .OutboxMessages.AsNoTracking().SingleAsync(Ct)).Attempts;
        if (afterConfirm)
            await EventuallyAsync(async () =>
            {
                await using var scope = _host.Services.CreateAsyncScope();
                return await scope.ServiceProvider.GetRequiredService<HistoricalRatesDbContext>().InboxMessages.AnyAsync(Ct);
            }, "confirmed first delivery committed before publisher loss");

        await _host.StopAsync(Ct);
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var current = scope.ServiceProvider.GetRequiredService<CurrentRatesDbContext>();
            var row = await current.OutboxMessages.AsNoTracking().SingleAsync(Ct);
            Assert.Null(row.ProcessedOnUtc);
            Assert.NotNull(row.LockOwner);
            Assert.NotNull(row.LockedUntilUtc);
            Assert.Equal(attemptsBeforeLoss, row.Attempts); // Cancellation leaves the interrupted publisher lease intact.
            var history = scope.ServiceProvider.GetRequiredService<HistoricalRatesDbContext>();
            Assert.Equal(afterConfirm ? 1 : 0, await history.InboxMessages.CountAsync(Ct));
        }
        _host.Dispose();
        _host = await CreateHostAsync();
        await _host.StartAsync(Ct);
        await EventuallyAsync(async () =>
        {
            await using var scope = _host.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CurrentRatesDbContext>()
                       .OutboxMessages.AllAsync(row => row.ProcessedOnUtc != null && row.LockOwner == null, Ct)
                   && await scope.ServiceProvider.GetRequiredService<HistoricalRatesDbContext>().InboxMessages.CountAsync(Ct) == 1;
        }, "restarted hosted publisher reclaimed lease and completed delivery");
        await AssertAcknowledgedAsync(afterConfirm ? 2 : 1);
        await using var verification = _host.Services.CreateAsyncScope();
        Assert.Single(await verification.ServiceProvider.GetRequiredService<HistoricalRatesDbContext>()
            .HistoricalFundingRates.AsNoTracking().ToListAsync(Ct));
        Assert.Equal(1, _market.Calls["BTC-USDT"]);
    }

    private async Task<IHost> CreateHostAsync(ConfirmWindow? fault = null)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
            ["RabbitMq:Host"] = _rabbit.Hostname,
            ["RabbitMq:Port"] = _rabbit.GetMappedPublicPort(5672).ToString(),
            ["RabbitMq:Username"] = "guest", ["RabbitMq:Password"] = "guest",
            ["CurrentDataCollectionOptions:UpdateIntervalSeconds"] = "3600",
            ["CurrentRatesOutbox:PollIntervalSeconds"] = "1",
            ["CurrentRatesOutbox:LeaseSeconds"] = "2",
            ["HistoricalDataCollectionOptions:DefaultConcurrencyPerExchange"] = "1",
            ["HistoricalDataCollectionOptions:ExchangeConcurrency:Binance"] = "1",
            ["HistoricalDataCollectionOptions:ExchangeConcurrency:Bybit"] = "1",
            ["HistoricalDataCollectionOptions:ExchangeConcurrency:OKX"] = "1",
            ["HistoricalDataCollectionOptions:RetryDelaySeconds"] = "1"
        });
        builder.Services.AddSingleton<IExchangeMarketDataModule>(_market);
        builder.Services.AddCurrentRatesApplication(builder.Configuration);
        builder.Services.AddCurrentRatesInfrastructure(builder.Configuration);
        builder.Services.AddHistoricalRatesApplication(builder.Configuration);
        builder.Services.AddHistoricalRatesInfrastructure(builder.Configuration);
        if (fault is not null)
        {
            builder.Services.RemoveAll<IMessagePublisher>();
            builder.Services.AddSingleton<RabbitMqMessagePublisher>();
            builder.Services.AddSingleton<IMessagePublisher>(provider =>
                new InterruptedPublisher(provider.GetRequiredService<RabbitMqMessagePublisher>(), fault));
        }
        var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();
        await new ModuleDatabaseMigrator(scope.ServiceProvider.GetRequiredService<CurrentRatesDbContext>(),
            scope.ServiceProvider.GetRequiredService<HistoricalRatesDbContext>()).MigrateAsync(Ct);
        return host;
    }

    private async Task CollectAndAssertTransactionAsync(int expected)
    {
        await using var scope = _host!.Services.CreateAsyncScope();
        var collected = await scope.ServiceProvider.GetRequiredService<ICurrentFundingRateCollector>().CollectAsync(Ct);
        Assert.Equal(expected, collected.EventsCount);
        var current = scope.ServiceProvider.GetRequiredService<CurrentRatesDbContext>();
        Assert.Equal(expected, await current.CurrentFundingRates.CountAsync(Ct));
        Assert.Equal(expected, await current.OutboxMessages.CountAsync(row => row.ProcessedOnUtc == null, Ct));
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<HistoricalRatesDbContext>().InboxMessages.ToListAsync(Ct));
    }

    private Task AssertAcknowledgedAsync(int expectedAcks) => EventuallyAsync(async () =>
    {
        // Management counters include unacked deliveries, unlike BasicGet/QueueDeclare message counts.
        using var response = await _management.GetAsync("/api/queues/%2F/historical-rates.collect.binance", Ct);
        response.EnsureSuccessStatusCode();
        var queue = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        return queue.TryGetProperty("messages_unacknowledged", out var unacked) && unacked.GetInt32() == 0
            && queue.TryGetProperty("messages_ready", out var ready) && ready.GetInt32() == 0
            && queue.TryGetProperty("message_stats", out var stats)
            && stats.TryGetProperty("ack", out var acknowledgements) && acknowledgements.GetInt32() >= expectedAcks
            && queue.GetProperty("consumers").GetInt32() == 1;
    }, "manual ACK: queue has one live hosted consumer and no ready or unacknowledged messages");

    private async Task EventuallyAsync(Func<Task<bool>> predicate, string expectation)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < TimeSpan.FromSeconds(30))
        {
            if (await predicate()) return;
            await Task.Delay(100, Ct);
        }
        Assert.Fail($"Timed out waiting for {expectation}.");
    }

    private sealed class ControlledMarketData : IExchangeMarketDataModule
    {
        public string[] Symbols { get; set; } = ["BTC-USDT"];
        public Dictionary<string, ErrorType> Failures { get; } = [];
        public ConcurrentDictionary<string, int> Calls { get; } = new();
        public Task<Result<IReadOnlyList<CurrentRateSnapshot>>> GetCurrentRatesAsync(ExchangeId exchange, CancellationToken cancellationToken) =>
            Task.FromResult(Result<IReadOnlyList<CurrentRateSnapshot>>.Success(exchange.Value == "Binance"
                ? Symbols.Select(symbol => new CurrentRateSnapshot(exchange, Symbol.Create(symbol), 100m, 100m,
                    0.0001m, 8, DateTimeOffset.UtcNow.AddHours(8), null, DateTimeOffset.UtcNow)).ToArray()
                : []));
        public Task<Result<IReadOnlyList<HistoricalRatePoint>>> GetHistoricalRatesAsync(HistoricalRatesRequest request, CancellationToken cancellationToken)
        {
            var calls = Calls.AddOrUpdate(request.Symbol.Value, 1, (_, value) => value + 1);
            if (Failures.TryGetValue(request.Symbol.Value, out var failure) && (failure == ErrorType.Validation || calls == 1))
                return Task.FromResult(Result<IReadOnlyList<HistoricalRatePoint>>.Failure(new Error("Controlled.Failure", "Controlled exchange failure", failure)));
            return Task.FromResult(Result<IReadOnlyList<HistoricalRatePoint>>.Success([
                new(request.Exchange, request.Symbol, 0.0003m, new DateTimeOffset(2026, 9, 8, 8, 0, 0, TimeSpan.Zero), DateTimeOffset.UtcNow)]));
        }
        public Task<IReadOnlyList<ExchangeHealthSnapshot>> CheckHealthAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class ConfirmWindow(bool afterConfirm)
    {
        public bool AfterConfirm { get; } = afterConfirm;
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class InterruptedPublisher(RabbitMqMessagePublisher realPublisher, ConfirmWindow fault) : IMessagePublisher
    {
        public async Task<MessagePublishResult> PublishAsync<T>(string exchange, string routingKey, MessageEnvelope<T> message,
            IReadOnlyDictionary<string, object?>? headers, CancellationToken cancellationToken)
        {
            if (fault.AfterConfirm)
            {
                var result = await realPublisher.PublishAsync(exchange, routingKey, message, headers, cancellationToken);
                if (result != MessagePublishResult.Success) return result;
            }
            fault.Reached.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new UnreachableException();
        }
    }
}
