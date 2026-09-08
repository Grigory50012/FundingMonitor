using FundingMonitor.HistoricalRates.Contracts;
using FundingMonitor.HistoricalRates.Presentation.Dtos;
using FundingMonitor.HistoricalRates.Presentation.Mapping;
using FundingMonitor.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FundingMonitor.HistoricalRates.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class HistoryController(IHistoricalRatesModule module) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<HistoricalFundingRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<HistoricalFundingRateDto>>> GetHistory(
        [FromQuery] string symbol,
        [FromQuery] string? exchanges,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? limit = 1000)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol is required", nameof(symbol));
        limit = limit > 1000 ? 1000 : limit;
        var result = await module.GetHistoryAsync(
            new HistoricalRatesQuery(
                Symbol.Create(symbol), ParseExchanges(exchanges),
                ToOffset(from), ToOffset(to), limit),
            HttpContext.RequestAborted);
        return Ok(result.ToDtoList());
    }

    [HttpGet("apr-stats")]
    [ProducesResponseType(typeof(List<AprPeriodStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<AprPeriodStatsDto>>> GetAprStats(
        [FromQuery] string symbol,
        [FromQuery] string? exchanges)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol is required", nameof(symbol));
        var normalized = symbol.Contains('-', StringComparison.Ordinal) ? symbol : $"{symbol}-USDT";
        var result = await module.GetAprStatsAsync(
            Symbol.Create(normalized), ParseExchanges(exchanges), HttpContext.RequestAborted);
        return Ok(result.ToDtoList());
    }

    private static DateTimeOffset? ToOffset(DateTime? value) => value is null
        ? null
        : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));

    private static IReadOnlyCollection<ExchangeId>? ParseExchanges(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => ParseExchange(item.Trim())).ToArray();
    }

    private static ExchangeId ParseExchange(string value)
    {
        try { return ExchangeId.Parse(value); }
        catch (ArgumentException)
        {
            throw new ArgumentException(
                $"Invalid exchange name: '{value}'. Valid values: Binance, Bybit, OKX");
        }
    }
}
