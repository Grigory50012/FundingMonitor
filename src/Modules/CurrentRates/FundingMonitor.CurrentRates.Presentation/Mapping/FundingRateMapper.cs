using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Presentation.Dtos;
using FundingMonitor.SharedKernel;

namespace FundingMonitor.CurrentRates.Presentation.Mapping;

internal static class FundingRateMapper
{
    public static List<FundingRateDto> ToDtoList(
        this IEnumerable<CurrentRateView> rates) => rates.Select(rate => new FundingRateDto(
        rate.Exchange.Value,
        rate.Symbol.Value,
        rate.MarkPrice,
        rate.FundingRate,
        rate.Apr,
        rate.FundingIntervalHours is > 0 ? 24 / rate.FundingIntervalHours.Value : 3,
        rate.NextFundingTime?.UtcDateTime,
        rate.Exchange.ToTradeUrl(rate.Symbol))).ToList();

    private static string ToTradeUrl(this ExchangeId exchange, Symbol symbol)
    {
        var compactSymbol = symbol.Value.Replace("-", string.Empty, StringComparison.Ordinal);
        var okxSymbol = $"{symbol.Value}-SWAP";
        return exchange.Value switch
        {
            "Binance" => $"https://www.binance.com/en/futures/{compactSymbol}",
            "Bybit" => $"https://www.bybit.com/trade/usdt/{compactSymbol}",
            "OKX" => $"https://www.okx.com/en/trade-swap/{okxSymbol.ToLowerInvariant()}",
            _ => string.Empty
        };
    }
}
