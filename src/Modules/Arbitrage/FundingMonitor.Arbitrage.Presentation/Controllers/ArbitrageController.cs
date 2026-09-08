using FundingMonitor.Arbitrage.Contracts;
using FundingMonitor.Arbitrage.Presentation.Dtos;
using FundingMonitor.Arbitrage.Presentation.Mapping;
using FundingMonitor.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FundingMonitor.Arbitrage.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class ArbitrageController(IArbitrageModule module) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<FundingArbitrageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FundingArbitrageDto>>> GetArbitrageOpportunities(
        [FromQuery] string? symbol = null,
        [FromQuery] string? exchanges = null)
    {
        var opportunities = await module.GetOpportunitiesAsync(
            new ArbitrageQuery(symbol, ParseExchanges(exchanges)),
            HttpContext.RequestAborted);
        return Ok(opportunities.ToDtoList());
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
