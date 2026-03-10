using EFCore.BulkExtensions;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Infrastructure.Data.Mappers;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.Infrastructure.Data.Repositories;

public class CurrentFundingRateRepository : ICurrentFundingRateRepository
{
    private readonly IDbContextFactory<FundingMonitorDbContext> _contextFactory;

    public CurrentFundingRateRepository(
        IDbContextFactory<FundingMonitorDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task UpdateAsync(IEnumerable<CurrentFundingRate> rates, CancellationToken cancellationToken)
    {
        var entities = rates.Select(FundingRateMapper.ToEntity).ToList();
        if (entities.Count == 0) return;

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var bulkConfig = new BulkConfig
        {
            UpdateByProperties = ["Exchange", "NormalizedSymbol"],
            TrackingEntities = false,
            BatchSize = 1000
        };

        await context.BulkInsertOrUpdateAsync(entities, bulkConfig, cancellationToken: cancellationToken);

        var incomingKeys = entities
            .Select(e => new { e.Exchange, e.NormalizedSymbol })
            .ToHashSet();

        await context.CurrentFundingRate
            .Where(r => !incomingKeys.Contains(new { r.Exchange, r.NormalizedSymbol }))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IEnumerable<CurrentFundingRate>> GetRatesAsync(
        string? symbol,
        List<ExchangeType>? exchanges,
        CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.CurrentFundingRate.AsNoTracking().Where(r => r.IsActive);

        if (exchanges?.Any() == true)
        {
            var exchangeNames = exchanges.Select(e => e.ToString()).ToHashSet();
            query = query.Where(r => exchangeNames.Contains(r.Exchange));
        }

        if (!string.IsNullOrEmpty(symbol))
        {
            query = query.Where(r => r.BaseAsset == symbol);
        }

        var entities = await query
            .OrderBy(r => r.NormalizedSymbol)
            .ThenBy(r => r.Exchange)
            .ToListAsync(cancellationToken);

        return FundingRateMapper.ToDomainList(entities);
    }
}