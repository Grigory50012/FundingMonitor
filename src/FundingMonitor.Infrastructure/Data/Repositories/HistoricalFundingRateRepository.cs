using EFCore.BulkExtensions;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Infrastructure.Data.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.Data.Repositories;

public class HistoricalFundingRateRepository : IHistoricalFundingRateRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<HistoricalFundingRateRepository> _logger;

    public HistoricalFundingRateRepository(
        AppDbContext context,
        ILogger<HistoricalFundingRateRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveRatesAsync(IEnumerable<HistoricalFundingRate> rates, CancellationToken cancellationToken)
    {
        var entities = rates.Select(HistoricalFundingRateMapper.ToEntity).ToList();
        if (entities.Count == 0) return;

        var bulkConfig = new BulkConfig
        {
            UpdateByProperties = new List<string> { "Exchange", "NormalizedSymbol", "FundingTime" },
            TrackingEntities = false,
            BatchSize = 4000,
            PropertiesToExcludeOnUpdate = new List<string> { "CollectedAt" }
        };

        await _context.BulkInsertOrUpdateAsync(entities, bulkConfig, cancellationToken: cancellationToken);
        _logger.LogDebug("Saved {Count} historical rates", entities.Count);
    }

    public async Task<HistoricalFundingRate?> GetLastRateAsync(
        string exchange,
        string normalizedSymbol,
        CancellationToken cancellationToken)
    {
        var entity = await _context.HistoricalFundingRate
            .Where(r => r.Exchange == exchange && r.NormalizedSymbol == normalizedSymbol)
            .OrderByDescending(r => r.FundingTime)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : HistoricalFundingRateMapper.ToDomain(entity);
    }

    public async Task<bool> HasHistoryAsync(
        string exchange,
        string normalizedSymbol,
        CancellationToken cancellationToken)
    {
        return await _context.HistoricalFundingRate
            .AnyAsync(r => r.Exchange == exchange && r.NormalizedSymbol == normalizedSymbol, cancellationToken);
    }
}