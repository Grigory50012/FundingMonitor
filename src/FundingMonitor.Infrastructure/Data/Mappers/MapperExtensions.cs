using FundingMonitor.Application.DTOs;
using FundingMonitor.Core.Entities;
using FundingMonitor.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.Infrastructure.Data.Mappers;

public static class MapperExtensions
{
    public static IQueryable<NormalizedFundingRate> ToDomainQuery(
        this IQueryable<NormalizedFundingRateEntity> query)
    {
        return query.Select(entity => new NormalizedFundingRate
        {
            Exchange = Enum.Parse<ExchangeType>(entity.Exchange),
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
    
    public static IQueryable<FundingRateDto> ToDtoQuery(
        this IQueryable<NormalizedFundingRateEntity> query)
    {
        return query.Select(entity => new FundingRateDto
        {
            Exchange = entity.Exchange,
            Symbol = entity.NormalizedSymbol,
            MarkPrice = entity.MarkPrice,
            IndexPrice = entity.IndexPrice,
            FundingRate = entity.FundingRate,
            FundingIntervalHours = entity.FundingIntervalHours,
            NextFundingTime = entity.NextFundingTime,
            PredictedRate = entity.PredictedNextRate,
            BaseAsset = entity.BaseAsset,
            QuoteAsset = entity.QuoteAsset
        });
    }
    
    public static async Task<List<NormalizedFundingRate>> ToDomainListAsync(
        this IQueryable<NormalizedFundingRateEntity> query)
    {
        var entities = await query.ToListAsync();
        return FundingRateMapper.ToDomainListFast(entities);
    }
    
    public static async Task<NormalizedFundingRate?> FirstOrDefaultDomainAsync(
        this IQueryable<NormalizedFundingRateEntity> query)
    {
        var entity = await query.FirstOrDefaultAsync();
        return entity == null ? null : FundingRateMapper.ToDomain(entity);
    }
}