using System.Diagnostics;
using EFCore.BulkExtensions;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Infrastructure.Data.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.Data.Repositories;

public class HistoricalFundingRateRepository : RepositoryBase, IHistoricalFundingRateRepository
{
    private readonly ILogger<HistoricalFundingRateRepository> _logger;

    public HistoricalFundingRateRepository(
        IDbContextFactory<FundingMonitorDbContext> contextFactory,
        ILogger<HistoricalFundingRateRepository> logger)
        : base(contextFactory)
    {
        _logger = logger;
    }

    public async Task AddRangeAsync(
        IEnumerable<HistoricalFundingRate> rates,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var entities = rates.Select(FundingRateMapper.ToEntity).ToList();
        if (entities.Count == 0) return;

        await using var context = await CreateContextAsync(cancellationToken);

        var bulkConfig = new BulkConfig
        {
            UpdateByProperties = new List<string> { "Exchange", "NormalizedSymbol", "FundingTime" },
            TrackingEntities = false,
            BatchSize = 1000,
            PropertiesToExcludeOnUpdate = new List<string> { "CollectedAt" }
        };

        try
        {
            await context.BulkInsertAsync(entities, bulkConfig, cancellationToken: cancellationToken);

            _logger.LogDebug("Saved: {Count} rates in {ElapsedMs}ms",
                entities.Count, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving {Count} rates", entities.Count);
            throw;
        }
    }

    public async Task<HistoricalFundingRate?> GetLastAsync(
        string exchange,
        string normalizedSymbol,
        CancellationToken cancellationToken)
    {
        await using var context = await CreateContextAsync(cancellationToken);

        var entity = await context.HistoricalFundingRate
            .Where(r => r.Exchange == exchange && r.NormalizedSymbol == normalizedSymbol)
            .OrderByDescending(r => r.FundingTime)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : FundingRateMapper.ToDomain(entity);
    }
}