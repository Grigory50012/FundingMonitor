using System.Collections.Concurrent;
using FundingMonitor.Core.Entities;
using FundingMonitor.Infrastructure.Data.Entities;

namespace FundingMonitor.Infrastructure.Data.Mappers;

public class HistoricalFundingRateMapper
{
    private static readonly ConcurrentDictionary<string, ExchangeType> ExchangeCache = new();

    public static HistoricalFundingRateEntity ToEntity(HistoricalFundingRate domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new HistoricalFundingRateEntity
        {
            Id = 0,
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

    public static List<HistoricalFundingRate> ToDomainList(List<HistoricalFundingRateEntity> entities)
    {
        if (entities is null || entities.Count == 0)
            return new List<HistoricalFundingRate>();

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