using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Core.Entities;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Application.Services;

public class FundingDataOrchestrator: IFundingDataService
{
    private readonly IDataCollector _collector;
    private readonly IArbitrageScanner _scanner;
    private readonly IExchangeHealthChecker _healthChecker;
    private readonly ILogger<FundingDataOrchestrator> _logger;
    
    public FundingDataOrchestrator(
        ILogger<FundingDataOrchestrator> logger, 
        IExchangeHealthChecker healthChecker, 
        IArbitrageScanner scanner, 
        IDataCollector collector)
    {
        _logger = logger;
        _healthChecker = healthChecker;
        _scanner = scanner;
        _collector = collector;
    }
    
    public async Task<List<NormalizedFundingRate>> CollectAllRatesAsync(CancellationToken ct)
        => await _collector.CollectAllRatesAsync(ct);
    
    public List<ArbitrageOpportunity> FindArbitrageOpportunitiesAsync(List<NormalizedFundingRate> rates)
        => _scanner.FindOpportunities(rates);
    
    public async Task<Dictionary<ExchangeType, bool>> CheckExchangesStatusAsync()
        => await _healthChecker.CheckAllExchangesAsync();
}