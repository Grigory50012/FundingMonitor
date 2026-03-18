using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Application.Services;

/// <summary>
///     Сервис для расчёта APR статистики по периодам
/// </summary>
public class AprStatsService : IAprStatsService
{
    // Периоды по умолчанию (в днях)
    private static readonly List<int> DefaultPeriods = [1, 2, 3, 7, 14, 21, 30];

    // Названия периодов на русском
    private static readonly Dictionary<int, string> PeriodLabels = new()
    {
        { 1, "1 день" },
        { 2, "2 дня" },
        { 3, "3 дня" },
        { 7, "7 дней" },
        { 14, "14 дней" },
        { 21, "21 день" },
        { 30, "30 дней" }
    };

    private readonly ILogger<AprStatsService> _logger;
    private readonly IHistoricalFundingRateRepository _repository;

    public AprStatsService(
        IHistoricalFundingRateRepository repository,
        ILogger<AprStatsService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<AprPeriodStats>> GetAprStatsAsync(
        string symbol,
        List<string>? exchanges,
        CancellationToken cancellationToken)
    {
        // Нормализуем символ (добавляем -USDT если нет)
        var normalizedSymbol = symbol.Contains("-USDT", StringComparison.OrdinalIgnoreCase)
            ? symbol
            : $"{symbol}-USDT";

        _logger.LogInformation(
            "Запрос APR статистики для символа {Symbol} (нормализованный: {NormalizedSymbol}), биржи: {Exchanges}, периоды: {Periods}",
            symbol,
            normalizedSymbol,
            exchanges != null ? string.Join(", ", exchanges) : "все",
            string.Join(", ", DefaultPeriods));

        // Получаем исторические данные
        var history = await _repository.GetHistoryAsync(
            normalizedSymbol,
            exchanges?.Select(e => Enum.Parse<ExchangeType>(e, true)).ToList(),
            null,
            null,
            10000, // Максимальное количество для покрытия всех периодов
            cancellationToken);

        if (history.Count == 0)
        {
            _logger.LogWarning("Нет исторических данных для символа {NormalizedSymbol}", normalizedSymbol);
            return [];
        }

        _logger.LogDebug("Получено {Count} записей для {Symbol}", history.Count, normalizedSymbol);

        // Группируем по биржам
        var exchangeData = history
            .GroupBy(h => h.Exchange)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(h => h.FundingTime).ToList());

        var result = new List<AprPeriodStats>();

        foreach (var (exchange, rates) in exchangeData)
        {
            // Получаем уникальные даты (только дата без времени)
            var uniqueDates = rates
                .Select(h => h.FundingTime.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            foreach (var days in DefaultPeriods)
            {
                // Берём первые N уникальных дат
                var datesToInclude = uniqueDates.Take(days).ToHashSet();

                // Фильтруем ставки по выбранным датам
                var ratesForPeriod = rates
                    .Where(h => datesToInclude.Contains(h.FundingTime.Date))
                    .ToList();

                if (ratesForPeriod.Count == 0)
                    continue;

                // Вычисляем метрики
                var totalFundingRate = ratesForPeriod.Sum(h => h.FundingRate);
                var apr = totalFundingRate * 100 * (365m / days);
                var avgFundingRate = totalFundingRate / ratesForPeriod.Count;

                var periodLabel = GetPeriodLabel(days);

                result.Add(new AprPeriodStats
                {
                    Exchange = exchange.ToString(),
                    Period = periodLabel,
                    Days = days,
                    Apr = apr,
                    TotalFundingRatePercent = totalFundingRate * 100,
                    PaymentsCount = ratesForPeriod.Count,
                    AvgFundingRatePercent = avgFundingRate * 100
                });
            }
        }

        _logger.LogInformation(
            "APR статистика для {Symbol}: {Count} записей",
            symbol,
            result.Count);

        return result;
    }

    private static string GetPeriodLabel(int days)
    {
        return PeriodLabels.TryGetValue(days, out var label)
            ? label
            : $"{days} дней";
    }
}