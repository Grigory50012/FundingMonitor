using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Presentation.Dtos;
using FundingMonitor.CurrentRates.Presentation.Mapping;
using FundingMonitor.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.CurrentRates.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class FundingRatesController(
    ICurrentRatesModule module,
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
        var exchangeList = ParseExchanges(exchanges);
        var rates = await module.GetCurrentRatesAsync(
            new CurrentRatesQuery(symbol, exchangeList),
            HttpContext.RequestAborted);
        logger.LogDebug("Current rates selected: {Count}", rates.Count);
        return Ok(rates.ToDtoList());
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
