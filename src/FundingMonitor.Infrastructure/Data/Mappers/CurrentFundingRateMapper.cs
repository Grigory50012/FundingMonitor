using System.Collections.Concurrent;
using FundingMonitor.Core.Entities;
using FundingMonitor.Infrastructure.Data.Entities;

namespace FundingMonitor.Infrastructure.Data.Mappers;

public static class CurrentFundingRateMapper
{
    private static readonly ConcurrentDictionary<string, ExchangeType> ExchangeCache = new();

    public static CurrentFundingRateEntity ToEntity(CurrentFundingRate domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new CurrentFundingRateEntity
        {
            Id = 0,
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

    public static List<CurrentFundingRate> ToDomainList(List<CurrentFundingRateEntity> entities)
    {
        if (entities is null || entities.Count == 0)
            return new List<CurrentFundingRate>();

        return entities.Select(ToDomain).ToList();
    }

    private static ExchangeType ParseExchange(string exchangeName)
    {
        return ExchangeCache.GetOrAdd(exchangeName, name =>
        {
            if (Enum.TryParse<ExchangeType>(name, true, out var result))
                return result;

            throw new InvalidOperationException($"Unknown exchange: {name}");
        });
    }
}