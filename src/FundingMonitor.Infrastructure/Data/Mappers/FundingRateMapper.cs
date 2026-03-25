using FundingMonitor.Core.Entities;
using FundingMonitor.Infrastructure.Data.Entities;

namespace FundingMonitor.Infrastructure.Data.Mappers;

/// <summary>
///     Extension-методы для маппинга между доменными моделями и EF Core сущностями
/// </summary>
public static class FundingRateMapperExtensions
{
    /// <summary>
    ///     Преобразовать доменную модель в EF Core сущность
    /// </summary>
    public static CurrentFundingRateDb ToDbEntity(this CurrentFundingRate domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new CurrentFundingRateDb
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

    /// <summary>
    ///     Преобразовать EF Core сущность в доменную модель
    /// </summary>
    public static CurrentFundingRate ToDomainModel(this CurrentFundingRateDb db)
    {
        ArgumentNullException.ThrowIfNull(db);

        return new CurrentFundingRate
        {
            Exchange = db.Exchange.ParseExchange(),
            NormalizedSymbol = db.NormalizedSymbol,
            BaseAsset = db.BaseAsset,
            QuoteAsset = db.QuoteAsset,
            MarkPrice = db.MarkPrice ?? 0,
            IndexPrice = db.IndexPrice ?? 0,
            FundingRate = db.FundingRate,
            FundingIntervalHours = db.FundingIntervalHours,
            NextFundingTime = db.NextFundingTime,
            LastCheck = db.LastCheck,
            PredictedNextRate = db.PredictedNextRate,
            IsActive = db.IsActive
        };
    }

    /// <summary>
    ///     Преобразовать коллекцию EF Core сущностей в список доменных моделей
    /// </summary>
    public static List<CurrentFundingRate> ToDomainModelList(this List<CurrentFundingRateDb>? entities)
    {
        return entities?.Select(ToDomainModel).ToList() ?? [];
    }

    /// <summary>
    ///     Преобразовать доменную модель истории в EF Core сущность
    /// </summary>
    public static HistoricalFundingRateDb ToDbEntity(this HistoricalFundingRate domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new HistoricalFundingRateDb
        {
            Exchange = domain.Exchange.ToString(),
            NormalizedSymbol = domain.NormalizedSymbol,
            FundingRate = domain.FundingRate,
            FundingTime = domain.FundingTime,
            CollectedAt = domain.CollectedAt
        };
    }

    /// <summary>
    ///     Преобразовать EF Core сущность истории в доменную модель
    /// </summary>
    public static HistoricalFundingRate ToDomainModel(this HistoricalFundingRateDb db)
    {
        ArgumentNullException.ThrowIfNull(db);

        return new HistoricalFundingRate
        {
            Exchange = db.Exchange.ParseExchange(),
            NormalizedSymbol = db.NormalizedSymbol,
            FundingRate = db.FundingRate,
            FundingTime = db.FundingTime,
            CollectedAt = db.CollectedAt
        };
    }

    /// <summary>
    ///     Преобразовать коллекцию EF Core сущностей истории в список доменных моделей
    /// </summary>
    public static List<HistoricalFundingRate> ToDomainModelList(this List<HistoricalFundingRateDb>? entities)
    {
        return entities?.Select(ToDomainModel).ToList() ?? [];
    }
}