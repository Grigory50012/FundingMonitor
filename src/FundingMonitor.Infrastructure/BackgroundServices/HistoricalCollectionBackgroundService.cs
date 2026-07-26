using System.Text.Json;
using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Queues;
using FundingMonitor.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using FundingExchangeType = FundingMonitor.Core.Entities.ExchangeType;

namespace FundingMonitor.Infrastructure.BackgroundServices;

public sealed class HistoricalCollectionBackgroundService : BackgroundService
{
    private readonly RabbitMqConnection _connection;
    private readonly IRabbitMqMessagePublisher _publisher;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HistoricalDataCollectionOptions _options;
    private readonly ILogger<HistoricalCollectionBackgroundService> _logger;

    public HistoricalCollectionBackgroundService(
        RabbitMqConnection connection,
        IRabbitMqMessagePublisher publisher,
        IServiceScopeFactory scopeFactory,
        IOptions<HistoricalDataCollectionOptions> options,
        ILogger<HistoricalCollectionBackgroundService> logger)
    {
        _connection = connection;
        _publisher = publisher;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workers = new List<Task>();
        foreach (var exchange in Enum.GetValues<FundingExchangeType>())
        {
            var concurrency = _options.GetConcurrency(exchange);
            for (var worker = 0; worker < concurrency; worker++)
            {
                var workerId = $"{exchange.ToString().ToLowerInvariant()}-{worker + 1}";
                workers.Add(RunWorkerAsync(exchange, workerId, stoppingToken));
            }
        }

        _logger.LogInformation(
            "Historical collection started with {WorkerCount} RabbitMQ workers",
            workers.Count);
        return Task.WhenAll(workers);
    }

    private async Task RunWorkerAsync(
        FundingExchangeType exchange,
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
                _logger.LogError(
                    exception,
                    "Historical worker {WorkerId} disconnected",
                    workerId);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task RunChannelAsync(
        FundingExchangeType exchange,
        string workerId,
        CancellationToken stoppingToken)
    {
        var connection = await _connection.GetAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: stoppingToken);
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        var restart = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        channel.ChannelShutdownAsync += (_, _) =>
        {
            restart.TrySetResult();
            return Task.CompletedTask;
        };

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            try
            {
                var disposition = await ProcessDeliveryAsync(
                    exchange,
                    workerId,
                    delivery,
                    stoppingToken);
                if (disposition == HistoricalMessageDisposition.Ack)
                {
                    await channel.BasicAckAsync(
                        delivery.DeliveryTag,
                        multiple: false,
                        cancellationToken: CancellationToken.None);
                }
                else
                {
                    restart.TrySetResult();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                restart.TrySetResult();
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Historical worker {WorkerId} failed before acknowledgement",
                    workerId);
                restart.TrySetResult();
            }
        };

        await channel.BasicConsumeAsync(
            RabbitMqTopology.GetQueueName(exchange),
            autoAck: false,
            consumerTag: workerId,
            noLocal: false,
            exclusive: false,
            arguments: null,
            consumer,
            cancellationToken: stoppingToken);

        await restart.Task.WaitAsync(stoppingToken);
    }

    private async Task<HistoricalMessageDisposition> ProcessDeliveryAsync(
        FundingExchangeType exchange,
        string workerId,
        BasicDeliverEventArgs delivery,
        CancellationToken stoppingToken)
    {
        HistoricalCollectionTask task;
        try
        {
            task = JsonSerializer.Deserialize<HistoricalCollectionTask>(
                       delivery.Body.Span)
                   ?? throw new JsonException("Historical task payload is null");
            if (task.Exchange != exchange ||
                task.TaskId == Guid.Empty ||
                string.IsNullOrWhiteSpace(task.NormalizedSymbol))
            {
                throw new JsonException(
                    "Historical task payload violates queue invariants");
            }
        }
        catch (JsonException exception)
        {
            _logger.LogError(
                exception,
                "Worker {WorkerId} received malformed message {MessageId}",
                workerId,
                delivery.BasicProperties.MessageId);
            return await PublishMalformedToDeadAsync(
                exchange,
                delivery,
                exception,
                stoppingToken);
        }

        using var scope = _scopeFactory.CreateScope();
        var processor = scope.ServiceProvider
            .GetRequiredService<HistoricalMessageProcessor>();
        using var logScope = _logger.BeginScope(
            new Dictionary<string, object?>
            {
                ["TaskId"] = task.TaskId,
                ["Exchange"] = task.Exchange,
                ["NormalizedSymbol"] = task.NormalizedSymbol,
                ["WorkerId"] = workerId,
                ["Attempt"] = task.RetryCount
            });
        return await processor.ProcessAsync(task, stoppingToken);
    }

    private async Task<HistoricalMessageDisposition> PublishMalformedToDeadAsync(
        FundingExchangeType exchange,
        BasicDeliverEventArgs delivery,
        JsonException exception,
        CancellationToken stoppingToken)
    {
        var taskId = Guid.TryParse(delivery.BasicProperties.MessageId, out var parsed)
            ? parsed
            : Guid.NewGuid();
        var placeholder = new HistoricalCollectionTask(
            taskId,
            exchange,
            "<invalid>",
            DateTime.UtcNow);
        RabbitMqPublishResult result;
        try
        {
            result = await _publisher.PublishAsync(
                placeholder,
                RabbitMqTopology.DeadRoutingKey,
                new Dictionary<string, object?>
                {
                    ["FailureCategory"] = "Permanent",
                    ["LastErrorType"] = exception.GetType().FullName,
                    ["LastErrorMessage"] = exception.Message,
                    ["FailedAt"] = DateTime.UtcNow.ToString("O")
                },
                stoppingToken);
        }
        catch (Exception publishException) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(
                publishException,
                "Malformed message {MessageId} could not be dead-lettered",
                delivery.BasicProperties.MessageId);
            return HistoricalMessageDisposition.LeaveUnacked;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return HistoricalMessageDisposition.LeaveUnacked;
        }

        return result.Confirmed && !result.Returned
            ? HistoricalMessageDisposition.Ack
            : HistoricalMessageDisposition.LeaveUnacked;
    }
}
