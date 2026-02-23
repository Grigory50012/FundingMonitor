using FundingMonitor.Core.Events;
using FundingMonitor.Core.Interfaces.Events;
using FundingMonitor.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace FundingMonitor.Console.Services;

public class RabbitMqInitializer : IHostedService
{
    private readonly List<IAsyncDisposable> _consumers = new();
    private readonly ILogger<RabbitMqInitializer> _logger;
    private readonly IServiceProvider _serviceProvider;

    public RabbitMqInitializer(
        IServiceProvider serviceProvider,
        ILogger<RabbitMqInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запуск RabbitMQ потребителя...");

        using var scope = _serviceProvider.CreateScope();

        var factory = scope.ServiceProvider.GetRequiredService<IConnectionFactory>();
        var newSymbolSubscriber = scope.ServiceProvider
            .GetRequiredService<IEventSubscriber<NewSymbolDetectedEvent>>();
        var timeChangedSubscriber = scope.ServiceProvider
            .GetRequiredService<IEventSubscriber<FundingTimeChangedEvent>>();

        try
        {
            var newSymbolConsumer = await RabbitMqEventConsumer<NewSymbolDetectedEvent>.CreateAsync(
                factory,
                newSymbolSubscriber,
                _logger,
                cancellationToken);

            var timeChangedConsumer = await RabbitMqEventConsumer<FundingTimeChangedEvent>.CreateAsync(
                factory,
                timeChangedSubscriber,
                _logger,
                cancellationToken);

            _consumers.Add(newSymbolConsumer);
            _consumers.Add(timeChangedConsumer);

            _logger.LogInformation("RabbitMQ потребитель успешно начал работу");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось запустить RabbitMQ потребителя");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
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

        _consumers.Clear();
        _logger.LogInformation("RabbitMQ consumers stopped");
    }
}