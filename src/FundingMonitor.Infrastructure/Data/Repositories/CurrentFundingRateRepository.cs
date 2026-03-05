using EFCore.BulkExtensions;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Infrastructure.Data.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.Data.Repositories;

public class CurrentFundingRateRepository : ICurrentFundingRateRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<CurrentFundingRateRepository> _logger;

    public CurrentFundingRateRepository(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<CurrentFundingRateRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task UpdateRatesAsync(IEnumerable<CurrentFundingRate> rates, CancellationToken cancellationToken)
    {
        var entities = rates.Select(FundingRateMapper.ToEntity).ToList();
        if (entities.Count == 0) return;

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var bulkConfig = new BulkConfig
        {
            UpdateByProperties = new List<string> { "Exchange", "NormalizedSymbol" },
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
        {
            await context.CurrentFundingRate
                .Where(r => r.Exchange == key.Exchange && r.NormalizedSymbol == key.NormalizedSymbol)
                .ExecuteDeleteAsync(cancellationToken);
        }

        _logger.LogDebug("Обновлено {Count} текущих ставок финансирования", entities.Count);
    }

    public async Task<IEnumerable<CurrentFundingRate>> GetRatesAsync(
        string? symbol,
        List<ExchangeType>? exchanges,
        CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.CurrentFundingRate.AsQueryable();

        if (exchanges?.Any() == true)
        {
            var exchangeNames = exchanges.Select(e => e.ToString()).ToList();
            query = query.Where(r => exchangeNames.Contains(r.Exchange));
        }

        if (!string.IsNullOrEmpty(symbol))
        {
            query = query.Where(r => r.BaseAsset == symbol);
        }

        query = query.Where(r => r.IsActive);
        query = query.OrderBy(r => r.NormalizedSymbol).ThenBy(r => r.Exchange);

        var entities = await query.ToListAsync(cancellationToken);
        return FundingRateMapper.ToDomainList(entities);
    }
}