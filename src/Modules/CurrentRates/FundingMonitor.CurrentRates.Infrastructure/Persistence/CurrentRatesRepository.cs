using FundingMonitor.CurrentRates.Application.Collection;
using FundingMonitor.CurrentRates.Application.Persistence;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Domain;
using FundingMonitor.CurrentRates.Infrastructure.Outbox;
using FundingMonitor.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FundingMonitor.CurrentRates.Infrastructure.Persistence;

internal sealed class CurrentRatesRepository(
    CurrentRatesDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<CurrentDataCollectionOptions> options)
    : ICurrentRatesRepository, ICurrentRatesUnitOfWork
{
    public async Task<IReadOnlyList<CurrentFundingRate>> GetRatesAsync(
        CurrentRatesQuery query,
        CancellationToken cancellationToken)
    {
        // Freshness must expire even when an exchange has no successful snapshots.
        var cutoff = timeProvider.GetUtcNow().AddMinutes(-options.Value.DeactivateMissingAfterMinutes);
        var databaseQuery = dbContext.CurrentFundingRates
            .AsNoTracking()
            .Where(item => item.IsActive && item.LastSeenOnUtc >= cutoff);

        if (query.Exchanges is { Count: > 0 })
        {
            var exchanges = query.Exchanges.Select(item => item.Value).ToArray();
            databaseQuery = databaseQuery.Where(item => exchanges.Contains(item.Exchange));
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

    public async Task<IReadOnlyList<CurrentFundingRate>> GetAllRatesAsync(
        CancellationToken cancellationToken) =>
        (await dbContext.CurrentFundingRates
                .AsNoTracking()
                .OrderBy(item => item.NormalizedSymbol)
                .ThenBy(item => item.Exchange)
                .ToListAsync(cancellationToken))
            .Select(Map)
            .ToArray();

    public async Task StoreSnapshotAsync(
        ExchangeId exchange,
        IReadOnlyCollection<CurrentFundingRate> rates,
        IReadOnlyCollection<FundingRateChangedIntegrationEvent> events,
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

        foreach (var rate in rates.Where(item => item.Exchange == exchange))
        {
            currentSymbols.Add(rate.Symbol.Value);
            if (!existingBySymbol.TryGetValue(rate.Symbol.Value, out var entity))
            {
                dbContext.CurrentFundingRates.Add(Map(rate));
                continue;
            }

            Apply(rate, entity);
        }

        var cutoff = timeProvider.GetUtcNow().AddMinutes(
            -options.Value.DeactivateMissingAfterMinutes);
        foreach (var missing in existing.Where(item =>
                     item.IsActive &&
                     !currentSymbols.Contains(item.NormalizedSymbol) &&
                     item.LastSeenOnUtc < cutoff))
            missing.IsActive = false;

        foreach (var integrationEvent in events)
        {
            dbContext.OutboxMessages.Add(new OutboxMessage
            {
                Id = integrationEvent.EventId,
                Type = typeof(FundingRateChangedIntegrationEvent).FullName!,
                Payload = OutboxEventSerializer.Serialize(integrationEvent),
                OccurredOnUtc = integrationEvent.OccurredOnUtc
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static CurrentFundingRateDb Map(CurrentFundingRate rate) => new()
    {
        Exchange = rate.Exchange.Value,
        NormalizedSymbol = rate.Symbol.Value,
        BaseAsset = rate.BaseAsset,
        QuoteAsset = rate.QuoteAsset,
        MarkPrice = rate.MarkPrice,
        IndexPrice = rate.IndexPrice,
        FundingRate = rate.FundingRate,
        FundingIntervalHours = rate.FundingIntervalHours,
        NextFundingTime = rate.NextFundingTime,
        PredictedNextRate = rate.PredictedNextRate,
        LastSeenOnUtc = rate.LastSeenOnUtc,
        IsActive = true
    };

    private static CurrentFundingRate Map(CurrentFundingRateDb entity) => new()
    {
        Exchange = ExchangeId.Parse(entity.Exchange),
        Symbol = Symbol.Create(entity.NormalizedSymbol),
        BaseAsset = entity.BaseAsset,
        QuoteAsset = entity.QuoteAsset,
        MarkPrice = entity.MarkPrice,
        IndexPrice = entity.IndexPrice,
        FundingRate = entity.FundingRate,
        FundingIntervalHours = entity.FundingIntervalHours,
        NextFundingTime = entity.NextFundingTime,
        PredictedNextRate = entity.PredictedNextRate,
        LastSeenOnUtc = entity.LastSeenOnUtc,
        IsActive = entity.IsActive
    };

    private static void Apply(CurrentFundingRate rate, CurrentFundingRateDb entity)
    {
        entity.BaseAsset = rate.BaseAsset;
        entity.QuoteAsset = rate.QuoteAsset;
        entity.MarkPrice = rate.MarkPrice;
        entity.IndexPrice = rate.IndexPrice;
        entity.FundingRate = rate.FundingRate;
        entity.FundingIntervalHours = rate.FundingIntervalHours;
        entity.NextFundingTime = rate.NextFundingTime;
        entity.PredictedNextRate = rate.PredictedNextRate;
        entity.LastSeenOnUtc = rate.LastSeenOnUtc;
        entity.IsActive = true;
    }
}
