using FundingMonitor.CurrentRates.CollectCurrentRates;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FundingMonitor.CurrentRates.Persistence;

internal sealed class CurrentRatesRepository(
    CurrentRatesDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<CurrentDataCollectionOptions> options)
    : ICurrentRatesRepository
{
    public async Task<IReadOnlyList<CurrentFundingRate>> GetRatesAsync(
        CurrentRatesQuery query,
        CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow().AddMinutes(-options.Value.DeactivateMissingAfterMinutes);
        var databaseQuery = dbContext.CurrentFundingRates
            .AsNoTracking()
            .Where(item => item.IsActive && item.LastSeenOnUtc >= cutoff);

        if (query.Exchanges is { Count: > 0 })
        {
            var exchanges = query.Exchanges.Select(item => item.Value).ToArray();
            databaseQuery = databaseQuery.Where(item => ((IEnumerable<string>)exchanges).Contains(item.Exchange));
        }

        if (!string.IsNullOrWhiteSpace(query.Symbol))
        {
            var baseAsset = query.Symbol.Trim().ToUpperInvariant();
            databaseQuery = databaseQuery.Where(item => item.BaseAsset == baseAsset);
        }

        return (await databaseQuery
                .OrderBy(item => item.NormalizedSymbol)
                .ThenBy(item => item.Exchange)
                .ToListAsync(cancellationToken))
            .Select(Map)
            .ToArray();
    }

    public async Task<int> StoreSnapshotAsync(
        ExchangeId exchange,
        IReadOnlyCollection<CurrentFundingRate> rates,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var exchangeName = exchange.Value;
        var existing = await dbContext.CurrentFundingRates
            .Where(item => item.Exchange == exchangeName)
            .ToListAsync(cancellationToken);
        var existingBySymbol = existing.ToDictionary(
            item => item.NormalizedSymbol,
            StringComparer.OrdinalIgnoreCase);
        var currentSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var now = timeProvider.GetUtcNow();
        var eventCount = 0;

        foreach (var rate in rates.Where(item => item.Exchange == exchange))
        {
            currentSymbols.Add(rate.Symbol.Value);
            existingBySymbol.TryGetValue(rate.Symbol.Value, out var entity);
            if (entity is null || entity.NextFundingTime != rate.NextFundingTime)
            {
                dbContext.OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Exchange = exchangeName,
                    Symbol = rate.Symbol.Value,
                    NextFundingTime = rate.NextFundingTime,
                    OccurredOnUtc = now
                });
                eventCount++;
            }

            if (entity is null)
            {
                dbContext.CurrentFundingRates.Add(CreateEntity(rate));
                continue;
            }

            Apply(rate, entity);
        }

        var cutoff = now.AddMinutes(
            -options.Value.DeactivateMissingAfterMinutes);
        foreach (var missing in existing.Where(item =>
                     item.IsActive &&
                     !currentSymbols.Contains(item.NormalizedSymbol) &&
                     item.LastSeenOnUtc < cutoff))
            missing.IsActive = false;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return eventCount;
    }

    private static CurrentFundingRateDb CreateEntity(CurrentFundingRate rate)
    {
        var entity = new CurrentFundingRateDb
        {
            Exchange = rate.Exchange.Value,
            NormalizedSymbol = rate.Symbol.Value,
            BaseAsset = rate.BaseAsset,
            QuoteAsset = rate.QuoteAsset
        };
        Apply(rate, entity);
        return entity;
    }

    private static CurrentFundingRate Map(CurrentFundingRateDb entity) => new()
    {
        Exchange = ExchangeId.Parse(entity.Exchange),
        Symbol = Symbol.Create(entity.NormalizedSymbol),
        BaseAsset = entity.BaseAsset,
        QuoteAsset = entity.QuoteAsset,
        MarkPrice = entity.MarkPrice,
        IndexPrice = entity.IndexPrice,
        BidPrice = entity.BidPrice,
        AskPrice = entity.AskPrice,
        FundingRate = entity.FundingRate,
        FundingIntervalHours = entity.FundingIntervalHours,
        NextFundingTime = entity.NextFundingTime,
        LastSeenOnUtc = entity.LastSeenOnUtc,
        IsActive = entity.IsActive
    };

    private static void Apply(CurrentFundingRate rate, CurrentFundingRateDb entity)
    {
        entity.BaseAsset = rate.BaseAsset;
        entity.QuoteAsset = rate.QuoteAsset;
        entity.MarkPrice = rate.MarkPrice;
        entity.IndexPrice = rate.IndexPrice;
        entity.BidPrice = rate.BidPrice;
        entity.AskPrice = rate.AskPrice;
        entity.FundingRate = rate.FundingRate;
        entity.FundingIntervalHours = rate.FundingIntervalHours;
        entity.NextFundingTime = rate.NextFundingTime;
        entity.LastSeenOnUtc = rate.LastSeenOnUtc;
        entity.IsActive = true;
    }
}
