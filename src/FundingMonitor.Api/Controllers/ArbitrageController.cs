using FundingMonitor.Api.Mappers;
using FundingMonitor.Api.Models.Dtos;
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
    ///     Получить арбитражные возможности по символу
    /// </summary>
    [HttpGet("symbol/{symbol}")]
    [ProducesResponseType(typeof(List<FundingArbitrageDto>), StatusCodes.Status200OK)]
    public ActionResult<List<FundingArbitrageDto>> GetBySymbol(string symbol)
    {
        return Ok(_service.GetBySymbol(symbol).ToArbitrageDtoList());
    }

    /// <summary>
    ///     Получить арбитражные возможности, отсортированные по разнице APR (по доходности)
    /// </summary>
    [HttpGet("sorted-by-apr")]
    [ProducesResponseType(typeof(List<FundingArbitrageDto>), StatusCodes.Status200OK)]
    public ActionResult<List<FundingArbitrageDto>> GetSortedByProfitabilityPercent()
    {
        return Ok(_service.GetSortedByAprDiff().ToArbitrageDtoList());
    }
}