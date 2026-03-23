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
    /// <param name="includeInactive">Включать неактивные символы</param>
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
        [FromQuery] string? exchanges,
        [FromQuery] bool includeInactive = false)
    {
        // Парсим биржи из строки
        var exchangeList = ParseExchanges(exchanges);

        _logger.LogInformation(
            "API Request - GetFundingRates - Symbol: {Symbol}, Exchanges: {Exchanges}, IncludeInactive: {IncludeInactive}",
            symbol, exchanges ?? "all", includeInactive);

        var stopwatch = Stopwatch.StartNew();

        var rates = await _repository.GetRatesAsync(
            symbol,
            exchangeList,
            CancellationToken.None);

        if (!includeInactive) rates = rates.Where(r => r.IsActive);

        _logger.LogInformation("API Response - GetFundingRates - Found {Count} rates in {ElapsedMs}ms",
            rates.Count(), stopwatch.ElapsedMilliseconds);

        return Ok(rates.ToDtoList());
    }

    private static List<ExchangeType>? ParseExchanges(string? exchanges)
    {
        if (string.IsNullOrWhiteSpace(exchanges))
            return null;

        var exchangeList = exchanges.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(e =>
            {
                if (!Enum.TryParse<ExchangeType>(e.Trim(), true, out var exchange))
                    throw new ArgumentException(
                        $"Invalid exchange name: '{e.Trim()}'. Valid values: Binance, Bybit, OKX");
                return exchange;
            })
            .ToList();

        return exchangeList;
    }
}