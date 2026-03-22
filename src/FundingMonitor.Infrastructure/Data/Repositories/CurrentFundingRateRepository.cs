using EFCore.BulkExtensions;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Infrastructure.Data.Mappers;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.Infrastructure.Data.Repositories;

public class CurrentFundingRateRepository : RepositoryBase, ICurrentFundingRateRepository
{
    public CurrentFundingRateRepository(
        IDbContextFactory<FundingMonitorDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    public async Task UpdateAsync(IEnumerable<CurrentFundingRate> rates, CancellationToken cancellationToken)
    {
        var entities = rates.Select(r => r.ToDbEntity()).ToList();
        if (entities.Count == 0) return;

        await using var context = await CreateContextAsync(cancellationToken);

        var bulkConfig = new BulkConfig
        {
            UpdateByProperties = ["Exchange", "NormalizedSymbol"],
            TrackingEntities = false,
            BatchSize = 1000
        };

        await context.BulkInsertOrUpdateAsync(entities, bulkConfig, cancellationToken: cancellationToken);

        // Удаляем отсутствующие символы
        var existingKeys = await context.CurrentFundingRate
            .Select(r => new { r.Exchange, r.NormalizedSymbol })
            .ToListAsync(cancellationToken);

        var incomingKeys = entities
            .Select(e => new { e.Exchange, e.NormalizedSymbol })
            .ToHashSet();

        var keysToDelete = existingKeys
            .Where(k => !incomingKeys.Contains(k))
            .ToList();

        foreach (var key in keysToDelete)
            await context.CurrentFundingRate
                .Where(r => r.Exchange == key.Exchange && r.NormalizedSymbol == key.NormalizedSymbol)
                .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IEnumerable<CurrentFundingRate>> GetRatesAsync(
        string? symbol,
        List<ExchangeType>? exchanges,
        CancellationToken cancellationToken)
    {
        await using var context = await CreateContextAsync(cancellationToken);

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

        return entities.ToDomainModelList();
    }
}