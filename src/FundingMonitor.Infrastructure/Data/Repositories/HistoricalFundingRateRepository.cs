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

        var entities = rates.Select(r => r.ToDbEntity()).ToList();
        if (entities.Count == 0) return;

        await using var context = await CreateContextAsync(cancellationToken);

        var bulkConfig = new BulkConfig
        {
            TrackingEntities = false,
            BatchSize = 1000
        };

        try
        {
            await context.BulkInsertOrUpdateAsync(entities, bulkConfig, cancellationToken: cancellationToken);

            _logger.LogDebug("Saved: {Count} rates in {ElapsedMs}ms",
                entities.Count, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving {Count} rates", entities.Count);
            throw;
        }
    }

    public async Task<List<HistoricalFundingRate>> GetHistoryAsync(
        string symbol,
        List<ExchangeType>? exchanges,
        DateTime? from,
        DateTime? to,
        int? limit,
        CancellationToken cancellationToken)
    {
        await using var context = await CreateContextAsync(cancellationToken);

        var query = context.HistoricalFundingRate.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(symbol)) query = query.Where(r => r.NormalizedSymbol == symbol);

        if (exchanges?.Any() == true)
        {
            var exchangeNames = exchanges.Select(e => e.ToString()).ToHashSet();
            query = query.Where(r => exchangeNames.Contains(r.Exchange));
        }

        if (from.HasValue) query = query.Where(r => r.FundingTime >= from.Value);
        if (to.HasValue) query = query.Where(r => r.FundingTime <= to.Value);

        query = query.OrderByDescending(r => r.FundingTime);

        if (limit.HasValue) query = query.Take(limit.Value);

        var entities = await query.ToListAsync(cancellationToken);

        return entities.ToDomainModelList();
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

        return entity is null ? null : entity.ToDomainModel();
    }
}