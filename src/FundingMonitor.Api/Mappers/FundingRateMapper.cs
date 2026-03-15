using FundingMonitor.Api.Models.Dtos;
using FundingMonitor.Core.Entities;

namespace FundingMonitor.Api.Mappers;

public static class FundingRateMapper
{
    public static FundingRateDto ToDto(CurrentFundingRate entity)
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
            IsActive = entity.IsActive
        };
    }

    public static List<FundingRateDto> ToDtoList(IEnumerable<CurrentFundingRate> entities)
    {
        return entities.Select(ToDto).ToList();
    }
}