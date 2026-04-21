using FundingMonitor.Api.Mappers;
using FundingMonitor.Api.Models.Dtos;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace FundingMonitor.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ArbitrageController : ControllerBase
{
    private readonly IFundingArbitrageService _service;

    public ArbitrageController(IFundingArbitrageService service)
    {
        _service = service;
    }

    /// <summary>
    ///     Получить арбитражные возможности с фильтрацией по символу и биржам
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<FundingArbitrageDto>), StatusCodes.Status200OK)]
    public ActionResult<List<FundingArbitrageDto>> GetArbitrageOpportunities(
        [FromQuery] string? symbol = null,
        [FromQuery] string? exchanges = null)
    {
        var exchangeList = exchanges.ParseExchanges();

        return Ok(_service.GetSortedByAprDiff(symbol, exchangeList).ToArbitrageDtoList());
    }
}