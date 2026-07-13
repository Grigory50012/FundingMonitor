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

    public async Task UpdateExchangeAsync(
        ExchangeType exchange,
        IEnumerable<CurrentFundingRate> rates,
        CancellationToken cancellationToken)
    {
        var entities = rates
            .Where(r => r.Exchange == exchange)
            .Select(r => (r with { IsActive = true }).ToDbEntity())
            .ToList();

        await using var context = await CreateContextAsync(cancellationToken);

        if (entities.Count != 0)
        {
            var bulkConfig = new BulkConfig
            {
                UpdateByProperties = ["Exchange", "NormalizedSymbol"],
                TrackingEntities = false,
                BatchSize = 1000
            };

            await context.BulkInsertOrUpdateAsync(entities, bulkConfig, cancellationToken: cancellationToken);
        }
    }

    public async Task DeactivateStaleAsync(
        ExchangeType exchange,
        TimeSpan deactivateMissingAfter,
        CancellationToken cancellationToken)
    {
        await using var context = await CreateContextAsync(cancellationToken);

        var cutoff = DateTime.UtcNow.Subtract(deactivateMissingAfter);
        var exchangeName = exchange.ToString();

        await context.CurrentFundingRate
            .Where(r => r.Exchange == exchangeName && r.IsActive && r.LastSeenAt < cutoff)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(r => r.IsActive, false),
                cancellationToken);
    }

    public async Task<IEnumerable<CurrentFundingRate>> GetRatesAsync(
        string? symbol,
        List<ExchangeType>? exchanges,
        CancellationToken cancellationToken)
    {
        await using var context = await CreateContextAsync(cancellationToken);

        var query = context.CurrentFundingRate
            .AsNoTracking()
            .Where(r => r.IsActive);

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

    public async Task<IEnumerable<CurrentFundingRate>> GetAllRatesAsync(CancellationToken cancellationToken)
    {
        await using var context = await CreateContextAsync(cancellationToken);

        var entities = await context.CurrentFundingRate
            .AsNoTracking()
            .OrderBy(r => r.NormalizedSymbol)
            .ThenBy(r => r.Exchange)
            .ToListAsync(cancellationToken);

        return entities.ToDomainModelList();
    }
}
