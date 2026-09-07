using FundingMonitor.ExchangeIntegration.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FundingMonitor.ExchangeIntegration.Presentation.Controllers;

[ApiController]
[Route("api/v1/exchanges")]
public sealed class ExchangesController(IExchangeMarketDataModule module) : ControllerBase
{
    [HttpGet("health")]
    [ProducesResponseType(typeof(Dictionary<string, bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, bool>>> GetHealth(
        CancellationToken cancellationToken)
    {
        var snapshots = await module.CheckHealthAsync(cancellationToken);
        return Ok(snapshots.ToDictionary(
            snapshot => snapshot.Exchange.Value,
            snapshot => snapshot.Status == ExchangeHealthStatus.Available,
            StringComparer.Ordinal));
    }
}
