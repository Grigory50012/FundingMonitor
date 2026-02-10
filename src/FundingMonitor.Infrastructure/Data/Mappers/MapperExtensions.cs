using FundingMonitor.Core.Entities;
using FundingMonitor.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.Infrastructure.Data.Mappers;

public static class MapperExtensions
{
    public static async Task<List<NormalizedFundingRate>> ToDomainListAsync(
        this IQueryable<NormalizedFundingRateEntity> query)
    {
        var entities = await query.ToListAsync();
        return FundingRateMapper.ToDomainListFast(entities);
    }
}