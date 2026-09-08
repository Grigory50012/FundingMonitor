using FundingMonitor.HistoricalRates.Contracts;
using FundingMonitor.HistoricalRates.Presentation.Dtos;

namespace FundingMonitor.HistoricalRates.Presentation.Mapping;

internal static class HistoricalRatesMapper
{
    public static List<HistoricalFundingRateDto> ToDtoList(
        this IEnumerable<HistoricalRateView> history) => history.Select(item =>
        new HistoricalFundingRateDto(
            item.Exchange.Value, item.Symbol.Value, item.FundingRate, item.FundingTime.UtcDateTime)).ToList();

    public static List<AprPeriodStatsDto> ToDtoList(
        this IEnumerable<AprPeriodStatsView> stats) => stats.Select(item =>
        new AprPeriodStatsDto(
            item.Exchange.Value, item.Period, item.Days, item.Apr,
            item.TotalFundingRatePercent, item.PaymentsCount,
            item.AvgFundingRatePercent, item.StdDev)).ToList();
}
