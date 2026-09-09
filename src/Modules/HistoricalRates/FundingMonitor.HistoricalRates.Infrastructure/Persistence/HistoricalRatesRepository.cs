using FundingMonitor.HistoricalRates.Application.Persistence;
using FundingMonitor.HistoricalRates.Contracts;
using FundingMonitor.HistoricalRates.Domain;
using FundingMonitor.HistoricalRates.Infrastructure.Inbox;
using FundingMonitor.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace FundingMonitor.HistoricalRates.Infrastructure.Persistence;

internal sealed class HistoricalRatesRepository(
    HistoricalRatesDbContext dbContext,
    TimeProvider timeProvider) : IHistoricalRatesRepository, IHistoricalRatesUnitOfWork
{
    public Task<bool> HasProcessedAsync(
        Guid eventId,
        string consumer,
        CancellationToken cancellationToken) =>
        dbContext.InboxMessages.AsNoTracking().AnyAsync(
            item => item.EventId == eventId && item.Consumer == consumer,
            cancellationToken);

    public async Task StoreHistoryAndMarkProcessedAsync(
        Guid eventId,
        string consumer,
        IReadOnlyCollection<HistoricalFundingRate> rates,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (rates.Count > 0)
                await UpsertHistoryAsync(rates, cancellationToken);

            dbContext.InboxMessages.Add(new InboxMessage
            {
                EventId = eventId,
                Consumer = consumer,
                ProcessedOnUtc = timeProvider.GetUtcNow()
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "PK_InboxMessages"
            })
        {
            await transaction.RollbackAsync(cancellationToken);
        }
    }

    private Task UpsertHistoryAsync(
        IReadOnlyCollection<HistoricalFundingRate> rates,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO historical_rates."HistoricalFundingRates"
                ("Exchange", "NormalizedSymbol", "FundingRate", "FundingTime", "CollectedOnUtc")
            SELECT *
            FROM unnest(
                @exchanges::text[],
                @symbols::text[],
                @funding_rates::numeric[],
                @funding_times::timestamptz[],
                @collected_on_utc::timestamptz[])
            ON CONFLICT ("Exchange", "NormalizedSymbol", "FundingTime")
            DO UPDATE SET
                "FundingRate" = EXCLUDED."FundingRate",
                "CollectedOnUtc" = EXCLUDED."CollectedOnUtc";
            """;
        var values = rates.ToArray();
        object[] parameters =
        [
            new NpgsqlParameter<string[]>("exchanges", values.Select(rate => rate.Exchange.Value).ToArray())
            {
                NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text
            },
            new NpgsqlParameter<string[]>("symbols", values.Select(rate => rate.Symbol.Value).ToArray())
            {
                NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text
            },
            new NpgsqlParameter<decimal[]>("funding_rates", values.Select(rate => rate.FundingRate).ToArray())
            {
                NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Numeric
            },
            new NpgsqlParameter<DateTimeOffset[]>("funding_times", values.Select(rate => rate.FundingTime).ToArray())
            {
                NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.TimestampTz
            },
            new NpgsqlParameter<DateTimeOffset[]>("collected_on_utc", values.Select(rate => rate.CollectedOnUtc).ToArray())
            {
                NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.TimestampTz
            }
        ];
        return dbContext.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
    }

    public async Task<IReadOnlyList<HistoricalFundingRate>> GetHistoryAsync(
        HistoricalRatesQuery query,
        CancellationToken cancellationToken)
    {
        var databaseQuery = dbContext.HistoricalFundingRates.AsNoTracking()
            .Where(item => item.NormalizedSymbol == query.Symbol.Value);
        if (query.Exchanges is { Count: > 0 })
        {
            var exchanges = query.Exchanges.Select(item => item.Value).ToArray();
            databaseQuery = databaseQuery.Where(item => exchanges.Contains(item.Exchange));
        }
        if (query.From is not null)
            databaseQuery = databaseQuery.Where(item => item.FundingTime >= query.From);
        if (query.To is not null)
            databaseQuery = databaseQuery.Where(item => item.FundingTime <= query.To);
        databaseQuery = databaseQuery.OrderByDescending(item => item.FundingTime);
        if (query.Limit is not null)
            databaseQuery = databaseQuery.Take(query.Limit.Value);

        return (await databaseQuery.ToListAsync(cancellationToken)).Select(item =>
            new HistoricalFundingRate(
                ExchangeId.Parse(item.Exchange),
                Symbol.Create(item.NormalizedSymbol),
                item.FundingRate,
                item.FundingTime,
                item.CollectedOnUtc)).ToArray();
    }
}
