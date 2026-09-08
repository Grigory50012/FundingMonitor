using FundingMonitor.HistoricalRates.Application.Persistence;
using FundingMonitor.HistoricalRates.Contracts;
using FundingMonitor.HistoricalRates.Domain;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FundingMonitor.HistoricalRates.Application;

public sealed class HistoricalRatesService(
    IHistoricalRatesRepository repository,
    IMemoryCache cache,
    IOptions<AprStatsOptions> options,
    TimeProvider timeProvider) : IHistoricalRatesModule
{
    private static readonly Dictionary<int, string> PeriodLabels = new()
    {
        [1] = "1 день", [2] = "2 дня", [3] = "3 дня", [7] = "7 дней",
        [14] = "14 дней", [21] = "21 день", [30] = "30 дней"
    };

    public async Task<IReadOnlyList<HistoricalRateView>> GetHistoryAsync(
        HistoricalRatesQuery query,
        CancellationToken cancellationToken) =>
        (await repository.GetHistoryAsync(query, cancellationToken))
        .Select(Map)
        .ToArray();

    public async Task<IReadOnlyList<AprPeriodStatsView>> GetAprStatsAsync(
        Symbol symbol,
        IReadOnlyCollection<ExchangeId>? exchanges,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"apr:{symbol.Value}:{(exchanges is null ? "all" : string.Join(',', exchanges.OrderBy(item => item.Value).Select(item => item.Value)))}";
        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.Value.CacheDurationMinutes);
            entry.Size = 1;
            var history = await repository.GetHistoryAsync(
                new HistoricalRatesQuery(
                    symbol,
                    exchanges,
                    timeProvider.GetUtcNow().AddDays(-options.Value.MaxHistoryDays),
                    null,
                    10000),
                cancellationToken);
            return Calculate(history, options.Value.Periods);
        }) ?? [];
    }

    private static IReadOnlyList<AprPeriodStatsView> Calculate(
        IReadOnlyList<HistoricalFundingRate> history,
        IReadOnlyCollection<int> periods)
    {
        var result = new List<AprPeriodStatsView>();
        foreach (var group in history.GroupBy(item => item.Exchange))
        {
            var rates = group.OrderByDescending(item => item.FundingTime).ToArray();
            var uniqueDates = rates.Select(item => item.FundingTime.UtcDateTime.Date)
                .Distinct().OrderByDescending(item => item).ToArray();
            foreach (var days in periods)
            {
                var dates = uniqueDates.Take(days).ToHashSet();
                var selected = rates.Where(item => dates.Contains(item.FundingTime.UtcDateTime.Date)).ToArray();
                if (selected.Length == 0)
                    continue;
                var total = selected.Sum(item => item.FundingRate);
                var average = total / selected.Length;
                var variance = selected.Select(item =>
                    Math.Pow((double)(item.FundingRate - average), 2)).Average();
                result.Add(new AprPeriodStatsView(
                    group.Key,
                    PeriodLabels.GetValueOrDefault(days, $"{days} дней"),
                    days,
                    total * 100m * (365m / days),
                    total * 100m,
                    selected.Length,
                    average * 100m,
                    (decimal)Math.Sqrt(variance) * 100m));
            }
        }
        return result;
    }

    private static HistoricalRateView Map(HistoricalFundingRate rate) => new(
        rate.Exchange, rate.Symbol, rate.FundingRate, rate.FundingTime, rate.CollectedOnUtc);
}
