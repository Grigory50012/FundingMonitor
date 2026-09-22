using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.HistoricalRates.Application;
using FundingMonitor.Messaging;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;

namespace FundingMonitor.HistoricalRates.Infrastructure.Messaging;

internal sealed class HistoricalCollectionBackgroundService(
    RabbitMqConnection connection,
    IServiceScopeFactory scopeFactory,
    IOptions<HistoricalDataCollectionOptions> options,
    ILogger<HistoricalCollectionBackgroundService> logger) : BackgroundService
{
    private static readonly ExchangeId[] Exchanges =
    [ExchangeId.Parse("Binance"), ExchangeId.Parse("Bybit"), ExchangeId.Parse("OKX")];

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workers = Exchanges.SelectMany(exchange =>
            Enumerable.Range(1, Math.Max(0, options.Value.GetConcurrency(exchange)))
                .Select(worker => RunWorkerAsync(exchange, $"{exchange.Value.ToLowerInvariant()}-{worker}", stoppingToken)));
        return Task.WhenAll(workers);
    }

    private async Task RunWorkerAsync(
        ExchangeId exchange,
        string workerId,
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunChannelAsync(exchange, workerId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Historical worker {WorkerId} disconnected", workerId);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task RunChannelAsync(
        ExchangeId exchange,
        string workerId,
        CancellationToken stoppingToken)
    {
        var rabbitConnection = await connection.GetAsync(stoppingToken);
        await using var channel = await rabbitConnection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.BasicQosAsync(0, 1, false, stoppingToken);
        var restart = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        channel.ChannelShutdownAsync += (_, _) => { restart.TrySetResult(); return Task.CompletedTask; };
        var consumer = new AsyncEventingBasicConsumer(channel);
        // basic.cancel (for example, queue deletion) does not necessarily close the channel.
        consumer.UnregisteredAsync += (_, _) => { restart.TrySetResult(); return Task.CompletedTask; };
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            try
            {
                MessageEnvelope<FundingRateChangedIntegrationEvent> envelope;
                int retryCount;
                try
                {
                    envelope = MessagingJson.Deserialize<MessageEnvelope<FundingRateChangedIntegrationEvent>>(
                                   delivery.Body.Span)
                               ?? throw new System.Text.Json.JsonException("Message envelope is empty.");
                    if (envelope.MessageId == Guid.Empty || envelope.Payload is null ||
                        envelope.Payload.EventId != envelope.MessageId ||
                        envelope.Payload.Exchange != exchange ||
                        envelope.MessageType != "funding-rate.changed" || envelope.Version != 1)
                        throw new System.Text.Json.JsonException("Message envelope violates queue invariants.");
                    retryCount = ReadRetryCount(delivery.BasicProperties.Headers);
                }
                catch (Exception exception)
                {
                    await using var malformedScope = scopeFactory.CreateAsyncScope();
                    var malformedDisposition = await malformedScope.ServiceProvider
                        .GetRequiredService<HistoricalMessageProcessor>()
                        .ProcessMalformedAsync(
                            exchange,
                            delivery.Body,
                            delivery.BasicProperties.MessageId,
                            exception,
                            stoppingToken);
                    if (malformedDisposition == HistoricalMessageDisposition.Ack)
                        await channel.BasicAckAsync(delivery.DeliveryTag, false, CancellationToken.None);
                    else
                        restart.TrySetResult();
                    return;
                }
                await using var scope = scopeFactory.CreateAsyncScope();
                var disposition = await scope.ServiceProvider
                    .GetRequiredService<HistoricalMessageProcessor>()
                    .ProcessAsync(envelope.Payload, retryCount, stoppingToken);
                if (disposition == HistoricalMessageDisposition.Ack)
                    await channel.BasicAckAsync(delivery.DeliveryTag, false, CancellationToken.None);
                else
                    restart.TrySetResult();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                restart.TrySetResult();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Historical worker {WorkerId} left message unacked", workerId);
                restart.TrySetResult();
            }
        };
        await channel.BasicConsumeAsync(
            RabbitMqTopology.GetQueueName(exchange), false, workerId, false, false,
            null, consumer, stoppingToken);
        await restart.Task.WaitAsync(stoppingToken);
    }

    private static int ReadRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue("x-retry-count", out var value))
            return 0;
        return value switch
        {
            byte number => number,
            short number => number,
            int number => number,
            long number => checked((int)number),
            _ => 0
        };
    }
}
