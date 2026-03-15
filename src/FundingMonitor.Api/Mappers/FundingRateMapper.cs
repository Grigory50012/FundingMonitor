using FundingMonitor.Api.Models.Dtos;
using FundingMonitor.Core.Entities;

namespace FundingMonitor.Api.Mappers;

public static class FundingRateMapper
{
    private static FundingRateDto ToDto(CurrentFundingRate entity)
    {
        return new FundingRateDto
        {
            Exchange = entity.Exchange.ToString(),
            Symbol = entity.NormalizedSymbol,
            MarkPrice = entity.MarkPrice,
            FundingRate = entity.FundingRate,
            APR = entity.APR,
            NumberOfPaymentsPerDay = entity.NumberOfPaymentsPerDay,
            NextFundingTime = entity.NextFundingTime,
        };
    }

    private static HistoricalFundingRateDto ToDto(HistoricalFundingRate entity)
    {
        return new HistoricalFundingRateDto
        {
            Exchange = entity.Exchange.ToString(),
            Symbol = entity.NormalizedSymbol,
            FundingRate = entity.FundingRate,
            FundingTime = entity.FundingTime
        };
    }

    public static List<FundingRateDto> ToDtoList(IEnumerable<CurrentFundingRate> entities)
    {
        return entities.Select(ToDto).ToList();
    }

    public static List<HistoricalFundingRateDto> ToDtoList(IEnumerable<HistoricalFundingRate> entities)
    {
        return entities.Select(ToDto).ToList();
    }
}