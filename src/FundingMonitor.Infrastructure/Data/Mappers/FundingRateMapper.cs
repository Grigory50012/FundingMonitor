using System.Collections.Concurrent;
using FundingMonitor.Core.Entities;
using FundingMonitor.Infrastructure.Data.Entities;

namespace FundingMonitor.Infrastructure.Data.Mappers;

public static class FundingRateMapper
{
    private static readonly ConcurrentDictionary<string, ExchangeType> ExchangeCache = new();

    public static CurrentFundingRateEntity ToEntity(CurrentFundingRate domainModel)
    {
        if (domainModel == null)
            throw new ArgumentNullException(nameof(domainModel), "Domain model cannot be null");

        return new CurrentFundingRateEntity
        {
            Id = 0, // Будет сгенерирован БД
            Exchange = domainModel.Exchange.ToString(),
            NormalizedSymbol = domainModel.NormalizedSymbol ??
                               throw new ArgumentException("NormalizedSymbol cannot be null", nameof(domainModel)),
            BaseAsset = domainModel.BaseAsset,
            QuoteAsset = domainModel.QuoteAsset,
            MarkPrice = domainModel.MarkPrice,
            IndexPrice = domainModel.IndexPrice,
            FundingRate = domainModel.FundingRate,
            FundingIntervalHours = domainModel.FundingIntervalHours ?? 8,
            NextFundingTime = domainModel.NextFundingTime,
            LastCheck = domainModel.LastCheck,
            PredictedNextRate = domainModel.PredictedNextRate,
            IsActive = domainModel.IsActive
        };
    }

    public static List<CurrentFundingRate> ToDomainListFast(
        List<CurrentFundingRateEntity>? entities)
    {
        if (entities is null || entities.Count is 0)
            return new List<CurrentFundingRate>();

        var result = new List<CurrentFundingRate>(entities.Count);

        foreach (var entity in entities)
        {
            result.Add(new CurrentFundingRate
            {
                Exchange = ParseExchangeCached(entity.Exchange),
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
            });
        }

        return result;
    }

    private static ExchangeType ParseExchangeCached(string exchangeName)
    {
        if (string.IsNullOrWhiteSpace(exchangeName))
            throw new ArgumentException("Exchange name cannot be empty", nameof(exchangeName));

        return ExchangeCache.GetOrAdd(exchangeName, name =>
        {
            if (Enum.TryParse<ExchangeType>(name, true, out var result))
                return result;

            throw new InvalidOperationException(
                $"Unknown exchange: '{name}'. Valid values: {string.Join(", ", Enum.GetNames<ExchangeType>())}");
        });
    }
}