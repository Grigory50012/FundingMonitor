using System.Diagnostics;
using System.Globalization;
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

        try
        {
            var distinctEntities = entities
                .GroupBy(e => new { e.Exchange, e.NormalizedSymbol, e.FundingTime })
                .Select(g => g.First())
                .ToList();

            if (distinctEntities.Count > 0)
            {
                var sql = @"
                    INSERT INTO ""HistoricalFundingRate"" (""Exchange"", ""NormalizedSymbol"", ""FundingTime"", ""CollectedAt"", ""FundingRate"")
                    VALUES {0}
                    ON CONFLICT (""Exchange"", ""NormalizedSymbol"", ""FundingTime"") DO UPDATE 
                    SET ""CollectedAt"" = EXCLUDED.""CollectedAt"", ""FundingRate"" = EXCLUDED.""FundingRate""";

                var values = string.Join(",\n", distinctEntities.Select(e =>
                    $"('{EscapeSqlString(e.Exchange)}', '{EscapeSqlString(e.NormalizedSymbol)}', '{e.FundingTime:O}', '{e.CollectedAt:O}', {e.FundingRate.ToString(CultureInfo.InvariantCulture)})"));

                var finalSql = string.Format(sql, values);

                await context.Database.ExecuteSqlRawAsync(finalSql, cancellationToken);
            }

            _logger.LogDebug("Saved: {Count} rates in {ElapsedMs}ms",
                distinctEntities.Count, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving {Count} rates", entities.Count);
            throw;
        }
    }

    public async Task<List<HistoricalFundingRate>> GetHistoryAsync(
        string? symbol,
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

        return entities.Select(FundingRateMapper.ToDomain).ToList();
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

    private static string EscapeSqlString(string input)
    {
        return input.Replace("'", "''");
    }
}