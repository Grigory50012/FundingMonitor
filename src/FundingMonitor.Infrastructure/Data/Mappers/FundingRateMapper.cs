using System.Collections.Concurrent;
using FundingMonitor.Core.Entities;
using FundingMonitor.Infrastructure.Data.Entities;

namespace FundingMonitor.Infrastructure.Data.Mappers;

public static class FundingRateMapper
{
    private static readonly ConcurrentDictionary<string, ExchangeType> ExchangeCache = new();

    // Current Funding Rate mappings
    public static CurrentFundingRateEntity ToEntity(CurrentFundingRate domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new CurrentFundingRateEntity
        {
            Exchange = domain.Exchange.ToString(),
            NormalizedSymbol = domain.NormalizedSymbol,
            BaseAsset = domain.BaseAsset,
            QuoteAsset = domain.QuoteAsset,
            MarkPrice = domain.MarkPrice,
            IndexPrice = domain.IndexPrice,
            FundingRate = domain.FundingRate,
            FundingIntervalHours = domain.FundingIntervalHours ?? 8,
            NextFundingTime = domain.NextFundingTime,
            LastCheck = domain.LastCheck,
            PredictedNextRate = domain.PredictedNextRate,
            IsActive = domain.IsActive
        };
    }

    public static CurrentFundingRate ToDomain(CurrentFundingRateEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new CurrentFundingRate
        {
            Exchange = ParseExchange(entity.Exchange),
            NormalizedSymbol = entity.NormalizedSymbol,
            BaseAsset = entity.BaseAsset,
            QuoteAsset = entity.QuoteAsset,
            MarkPrice = entity.MarkPrice ?? 0,
            IndexPrice = entity.IndexPrice ?? 0,
            FundingRate = entity.FundingRate,
            FundingIntervalHours = entity.FundingIntervalHours,
            NextFundingTime = entity.NextFundingTime,
            LastCheck = entity.LastCheck,
            PredictedNextRate = entity.PredictedNextRate,
            IsActive = entity.IsActive
        };
    }

    public static List<CurrentFundingRate> ToDomainList(List<CurrentFundingRateEntity>? entities)
    {
        return entities?.Select(ToDomain).ToList() ?? [];
    }

    // Historical Funding Rate mappings
    public static HistoricalFundingRateEntity ToEntity(HistoricalFundingRate domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new HistoricalFundingRateEntity
        {
            Exchange = domain.Exchange.ToString(),
            NormalizedSymbol = domain.NormalizedSymbol,
            FundingRate = domain.FundingRate,
            FundingTime = domain.FundingTime,
            CollectedAt = domain.CollectedAt
        };
    }

    public static HistoricalFundingRate ToDomain(HistoricalFundingRateEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new HistoricalFundingRate
        {
            Exchange = ParseExchange(entity.Exchange),
            NormalizedSymbol = entity.NormalizedSymbol,
            FundingRate = entity.FundingRate,
            FundingTime = entity.FundingTime,
            CollectedAt = entity.CollectedAt
        };
    }

    private static ExchangeType ParseExchange(string exchangeName)
    {
        return ExchangeCache.GetOrAdd(exchangeName, name =>
            Enum.TryParse<ExchangeType>(name, true, out var result)
                ? result
                : throw new InvalidOperationException($"Unknown exchange: {name}"));
    }
}