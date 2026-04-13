using FundingMonitor.Api.Models.Dtos;
using FundingMonitor.Core.Entities;

namespace FundingMonitor.Api.Mappers;

/// <summary>
///     Extension-методы для маппинга арбитражных моделей в DTO
/// </summary>
public static class ArbitrageMapper
{
    /// <summary>
    ///     Преобразовать доменную модель в DTO
    /// </summary>
    private static FundingArbitrageDto ToDto(this FundingArbitrageOpportunity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new FundingArbitrageDto(
            entity.Symbol,
            entity.ExchangeA.ToString(),
            entity.ExchangeB.ToString(),
            entity.PriceA,
            entity.PriceB,
            entity.PriceSpread,
            entity.PriceSpreadPercent,
            entity.FundingRateA,
            entity.FundingRateB,
            entity.FundingRateSpread,
            entity.PaymentsA,
            entity.PaymentsB,
            entity.ShortExchange.ToString(),
            entity.LongExchange.ToString()
        );
    }

    /// <summary>
    ///     Преобразовать коллекцию в список DTO
    /// </summary>
    public static List<FundingArbitrageDto> ToArbitrageDtoList(this IEnumerable<FundingArbitrageOpportunity> entities)
    {
        return entities.Select(ToDto).ToList();
    }
}