using System.Diagnostics;
using FundingMonitor.Api.Mappers;
using FundingMonitor.Api.Models.Dtos;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FundingMonitor.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class FundingRatesController : ControllerBase
{
    private readonly ILogger<FundingRatesController> _logger;
    private readonly ICurrentFundingRateRepository _repository;

    public FundingRatesController(
        ICurrentFundingRateRepository repository,
        ILogger<FundingRatesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    ///     Получить текущие ставки финансирования
    /// </summary>
    /// <param name="symbol">Символ (опционально, например: BTC)</param>
    /// <param name="exchanges">Список бирж (опционально, через запятую: Binance,Bybit)</param>
    /// <returns>Список текущих ставок</returns>
    /// <response code="200">Успешное получение данных</response>
    /// <response code="400">Неверные параметры запроса</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<FundingRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<FundingRateDto>>> GetFundingRates(
        [FromQuery] string? symbol,
        [FromQuery] string? exchanges)
    {
        var exchangeList = exchanges.ParseExchanges();

        _logger.LogDebug("GetFundingRates: symbol={Symbol}, exchanges={Exchanges}",
            symbol, exchanges ?? "all");

        var stopwatch = Stopwatch.StartNew();

        var rates = await _repository.GetRatesAsync(
            symbol,
            exchangeList,
            HttpContext.RequestAborted);

        stopwatch.Stop();
        _logger.LogInformation("GetFundingRates completed: {Count} rates in {Elapsed}ms",
            rates.Count(), stopwatch.ElapsedMilliseconds);

        return Ok(rates.ToDtoList());
    }
}
