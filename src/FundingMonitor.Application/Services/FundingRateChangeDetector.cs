using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Events;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Application.Services;

/// <summary>
///     Сервис для детектирования изменений в ставках финансирования
/// </summary>
public class FundingRateChangeDetector : IFundingRateChangeDetector
{
    private readonly ILogger<FundingRateChangeDetector> _logger;

    public FundingRateChangeDetector(ILogger<FundingRateChangeDetector> logger)
    {
        _logger = logger;
    }

    public List<FundingRateChangedEvent> DetectChanges(
        ExchangeType exchange,
        Dictionary<string, CurrentFundingRate> previous,
        List<CurrentFundingRate> current)
    {
        var events = new List<FundingRateChangedEvent>(current.Count);

        foreach (var rate in current)
            if (!previous.TryGetValue(rate.NormalizedSymbol, out var prev))
            {
                // Новый символ
                events.Add(new FundingRateChangedEvent
                {
                    Exchange = exchange,
                    NormalizedSymbol = rate.NormalizedSymbol,
                    NextFundingTime = rate.NextFundingTime
                });
                _logger.LogInformation("+ New symbol detected: {Exchange}:{Symbol}",
                    exchange, rate.NormalizedSymbol);
            }
            else if (rate.NextFundingTime != prev.NextFundingTime)
            {
                // Изменение времени выплаты
                events.Add(new FundingRateChangedEvent
                {
                    Exchange = exchange,
                    NormalizedSymbol = rate.NormalizedSymbol,
                    NextFundingTime = rate.NextFundingTime
                });
                _logger.LogInformation("Funding time changed: {Exchange}:{Symbol} {Old:HH:mm}->{New:HH:mm}",
                    exchange, rate.NormalizedSymbol, prev.NextFundingTime, rate.NextFundingTime);
            }

        return events;
    }
}