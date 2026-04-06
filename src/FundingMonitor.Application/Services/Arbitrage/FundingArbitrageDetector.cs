using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Application.Services.Arbitrage;

/// <summary>
///     Детектор межбиржевого арбитража ставок финансирования
/// </summary>
public class FundingArbitrageDetector : IFundingArbitrageDetector
{
    private readonly ILogger<FundingArbitrageDetector> _logger;
    private readonly decimal _minSpreadPercent;
    private readonly ICurrentFundingRateRepository _repository;

    public FundingArbitrageDetector(
        ICurrentFundingRateRepository repository,
        ILogger<FundingArbitrageDetector> logger,
        IOptions<FundingArbitrageOptions> options)
    {
        _repository = repository;
        _logger = logger;
        _minSpreadPercent = options.Value.MinSpreadPercent;
    }

    public async Task<IReadOnlyList<FundingArbitrageOpportunity>> DetectAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting funding arbitrage detection");

        var allRates = await _repository.GetRatesAsync(null, null, cancellationToken);
        var ratesList = allRates.ToList();

        if (ratesList.Count == 0)
        {
            _logger.LogDebug("No funding rate data available");
            return Array.Empty<FundingArbitrageOpportunity>();
        }

        var opportunities = new List<FundingArbitrageOpportunity>();
        var groupedBySymbol = ratesList.GroupBy(r => r.NormalizedSymbol);

        foreach (var symbolGroup in groupedBySymbol)
        {
            var rates = symbolGroup.ToList();
            if (rates.Count < 2)
                continue;

            for (var i = 0; i < rates.Count; i++)
            for (var j = i + 1; j < rates.Count; j++)
            {
                var rateA = rates[i];
                var rateB = rates[j];

                if (rateA.Exchange == rateB.Exchange)
                    continue;

                var aprDiff = Math.Abs(rateA.APR - rateB.APR);
                if (aprDiff < _minSpreadPercent)
                    continue;

                opportunities.Add(new FundingArbitrageOpportunity
                {
                    Symbol = symbolGroup.Key,
                    ExchangeA = rateA.Exchange,
                    ExchangeB = rateB.Exchange,
                    PriceA = rateA.MarkPrice,
                    PriceB = rateB.MarkPrice,
                    FundingRateA = rateA.FundingRate,
                    FundingRateB = rateB.FundingRate,
                    APRFundingRateA = rateA.APR,
                    APRFundingRateB = rateB.APR,
                    PaymentsA = rateA.NumberOfPaymentsPerDay,
                    PaymentsB = rateB.NumberOfPaymentsPerDay
                });

                _logger.LogDebug(
                    "Found: {Symbol} | {A}: {AprA:F2}% vs {B}: {AprB:F2}% | Diff: {Diff:F2}%",
                    symbolGroup.Key, rateA.Exchange, rateA.APR, rateB.Exchange, rateB.APR, aprDiff);
            }
        }

        _logger.LogDebug("Detection completed: found {Count} opportunities", opportunities.Count);
        return opportunities;
    }
}