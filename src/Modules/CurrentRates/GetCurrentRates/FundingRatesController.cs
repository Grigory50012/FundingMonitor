using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.CurrentRates.GetCurrentRates;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class FundingRatesController(
    ICurrentRatesModule currentRates,
    ILogger<FundingRatesController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<FundingRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<FundingRateDto>>> GetFundingRates(
        [FromQuery] string? symbol,
        [FromQuery] string? exchanges)
    {
        var selectedExchanges = ParseExchanges(exchanges);
        var rates = await currentRates.GetCurrentRatesAsync(
            new CurrentRatesQuery(symbol, selectedExchanges),
            HttpContext.RequestAborted);
        logger.LogDebug("Current rates selected: {Count}", rates.Count);
        return Ok(rates.Select(ToDto).ToList());
    }

    private static FundingRateDto ToDto(CurrentRateView rate) => new(
        rate.Exchange.Value,
        rate.Symbol.Value,
        rate.MarkPrice,
        rate.FundingRate ?? 0m,
        rate.Apr,
        rate.FundingIntervalHours is > 0 ? 24 / rate.FundingIntervalHours.Value : 3,
        rate.NextFundingTime?.UtcDateTime,
        GetTradeUrl(rate.Exchange, rate.Symbol));

    private static string GetTradeUrl(ExchangeId exchange, Symbol symbol)
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

    private static IReadOnlyCollection<ExchangeId>? ParseExchanges(string? exchanges)
    {
        if (string.IsNullOrWhiteSpace(exchanges))
            return null;

        return exchanges
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseExchange)
            .ToArray();
    }

    private static ExchangeId ParseExchange(string value)
    {
        var exchangeName = value.Trim();
        try
        {
            return ExchangeId.Parse(exchangeName);
        }
        catch (ArgumentException)
        {
            throw new ArgumentException(
                $"Invalid exchange name: '{exchangeName}'. Valid values: Binance, Bybit, OKX");
        }
    }
}
