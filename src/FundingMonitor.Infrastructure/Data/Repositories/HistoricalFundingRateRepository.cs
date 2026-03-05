using System.Diagnostics;
using EFCore.BulkExtensions;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Infrastructure.Data.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.Data.Repositories;

public class HistoricalFundingRateRepository : IHistoricalFundingRateRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<HistoricalFundingRateRepository> _logger;

    public HistoricalFundingRateRepository(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<HistoricalFundingRateRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task SaveRatesAsync(
        IEnumerable<HistoricalFundingRate> rates,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var entities = rates.Select(HistoricalFundingRateMapper.ToEntity).ToList();
        if (entities.Count == 0) return;

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

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

            _logger.LogDebug("Сохранено: {Count} ставок за {ElapsedMs}мс",
                entities.Count, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения {Count} ставок", entities.Count);
            throw;
        }
    }

    public async Task<HistoricalFundingRate?> GetLastRateAsync(
        string exchange,
        string normalizedSymbol,
        CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.HistoricalFundingRate
            .Where(r => r.Exchange == exchange && r.NormalizedSymbol == normalizedSymbol)
            .OrderByDescending(r => r.FundingTime)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : HistoricalFundingRateMapper.ToDomain(entity);
    }
}