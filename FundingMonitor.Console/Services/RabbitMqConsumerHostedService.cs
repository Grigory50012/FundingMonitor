using FundingMonitor.Core.Events;
using FundingMonitor.Core.Interfaces.Events;
using FundingMonitor.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace FundingMonitor.Console.Services;

public class RabbitMqConsumerHostedService : BackgroundService
{
    private readonly List<IAsyncDisposable> _consumers = new();
    private readonly ILogger<RabbitMqConsumerHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public RabbitMqConsumerHostedService(
        IServiceProvider serviceProvider,
        ILogger<RabbitMqConsumerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting RabbitMQ consumers...");

        using var scope = _serviceProvider.CreateScope();

        var factory = scope.ServiceProvider.GetRequiredService<IConnectionFactory>();
        var newSymbolSubscriber = scope.ServiceProvider
            .GetRequiredService<IEventSubscriber<NewSymbolDetectedEvent>>();
        var timeChangedSubscriber = scope.ServiceProvider
            .GetRequiredService<IEventSubscriber<FundingTimeChangedEvent>>();

        try
        {
            var newSymbolConsumer = await RabbitMqEventConsumer<NewSymbolDetectedEvent>.CreateAsync(
                factory, newSymbolSubscriber,
                _logger);

            var timeChangedConsumer = await RabbitMqEventConsumer<FundingTimeChangedEvent>.CreateAsync(
                factory, timeChangedSubscriber,
                _logger);

            _consumers.Add(newSymbolConsumer);
            _consumers.Add(timeChangedConsumer);

            _logger.LogInformation("RabbitMQ consumers started");

            // Keep running
            while (!stoppingToken.IsCancellationRequested) await Task.Delay(1000, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RabbitMQ consumers");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ consumers...");

        foreach (var consumer in _consumers)
            try
            {
                await consumer.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing consumer");
            }

        await base.StopAsync(cancellationToken);
    }
}