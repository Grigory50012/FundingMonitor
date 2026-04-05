using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Application.BackgroundServices;

public class CurrentCollectionBackgroundService : BackgroundService
{
    private readonly ILogger<CurrentCollectionBackgroundService> _logger;
    private readonly IOptions<CurrentDataCollectionOptions> _options;
    private readonly IServiceScopeFactory _scopeFactory;

    public CurrentCollectionBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<CurrentCollectionBackgroundService> logger,
        IOptions<CurrentDataCollectionOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CurrentCollectionBackgroundService started");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.Value.UpdateIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var collector = scope.ServiceProvider.GetRequiredService<ICurrentFundingRateCollector>();
            var detector = scope.ServiceProvider.GetRequiredService<IFundingArbitrageDetector>();
            var cache = scope.ServiceProvider.GetRequiredService<IFundingArbitrageService>();

            await collector.CollectFundingRatesAsync(stoppingToken);

            try
            {
                var opportunities = await detector.DetectAsync(stoppingToken);
                cache.UpdateOpportunities(opportunities);
                _logger.LogDebug("Arbitrage: {Count} opportunities", opportunities.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Arbitrage detection error: {Message}", ex.Message);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CurrentCollectionBackgroundService stopping...");
        await base.StopAsync(cancellationToken);
    }
}