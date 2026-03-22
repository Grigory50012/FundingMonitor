using FundingMonitor.Api.Models.Dtos;
using FundingMonitor.Core.Entities;

namespace FundingMonitor.Api.Mappers;

/// <summary>
///     Extension-методы для маппинга доменных моделей в DTO
/// </summary>
public static class FundingRateMapperExtensions
{
    /// <summary>
    ///     Преобразовать доменную модель в DTO
    /// </summary>
    public static FundingRateDto ToDto(this CurrentFundingRate entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new FundingRateDto(
            entity.Exchange.ToString(),
            entity.NormalizedSymbol,
            entity.MarkPrice,
            entity.FundingRate,
            entity.APR,
            entity.NumberOfPaymentsPerDay,
            entity.NextFundingTime
        );
    }

    /// <summary>
    ///     Преобразовать доменную модель истории в DTO
    /// </summary>
    public static HistoricalFundingRateDto ToDto(this HistoricalFundingRate entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new HistoricalFundingRateDto(
            entity.Exchange.ToString(),
            entity.NormalizedSymbol,
            entity.FundingRate,
            entity.FundingTime
        );
    }

    /// <summary>
    ///     Преобразовать доменную модель APR статистики в DTO
    /// </summary>
    public static AprPeriodStatsDto ToDto(this AprPeriodStats entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AprPeriodStatsDto(
            entity.Exchange,
            entity.Period,
            entity.Days,
            entity.Apr,
            entity.TotalFundingRatePercent,
            entity.PaymentsCount,
            entity.AvgFundingRatePercent
        );
    }

    /// <summary>
    ///     Преобразовать коллекцию доменных моделей в список DTO
    /// </summary>
    public static List<FundingRateDto> ToDtoList(this IEnumerable<CurrentFundingRate> entities)
    {
        return entities.Select(ToDto).ToList();
    }

    /// <summary>
    ///     Преобразовать коллекцию исторических моделей в список DTO
    /// </summary>
    public static List<HistoricalFundingRateDto> ToDtoList(this IEnumerable<HistoricalFundingRate> entities)
    {
        return entities.Select(ToDto).ToList();
    }

    /// <summary>
    ///     Преобразовать коллекцию APR статистики в список DTO
    /// </summary>
    public static List<AprPeriodStatsDto> ToDtoList(this IEnumerable<AprPeriodStats> entities)
    {
        return entities.Select(ToDto).ToList();
    }
}