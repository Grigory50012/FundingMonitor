using System.Diagnostics;
using FundingMonitor.Api.Mappers;
using FundingMonitor.Api.Models.Dtos;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace FundingMonitor.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HistoryController : ControllerBase
{
    private readonly IAprStatsService _aprStatsService;
    private readonly ILogger<HistoryController> _logger;
    private readonly IHistoricalFundingRateRepository _repository;

    public HistoryController(
        IHistoricalFundingRateRepository repository,
        ILogger<HistoryController> logger,
        IAprStatsService aprStatsService)
    {
        _repository = repository;
        _logger = logger;
        _aprStatsService = aprStatsService;
    }

    /// <summary>
    ///     Получить исторические ставки финансирования
    /// </summary>
    /// <param name="symbol">Символ (обязательно, например: BTC-USDT)</param>
    /// <param name="exchanges">Список бирж (опционально, через запятую)</param>
    /// <param name="from">Начальная дата (опционально, ISO формат)</param>
    /// <param name="to">Конечная дата (опционально, ISO формат)</param>
    /// <param name="limit">Максимальное количество записей (по умолчанию 1000, макс 1000)</param>
    /// <returns>Исторические ставки</returns>
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

        if (limit > 1000) limit = 1000;

        _logger.LogDebug("GetHistory: symbol={Symbol}, exchanges={Exchanges}, from={From}, to={To}, limit={Limit}",
            symbol, exchanges, from, to, limit);

        var stopwatch = Stopwatch.StartNew();

        var exchangeList = exchanges.ParseExchanges();

        var history = await
            _repository.GetHistoryAsync(symbol, exchangeList, from, to, limit, CancellationToken.None);

        stopwatch.Stop();
        _logger.LogInformation("GetHistory completed: {Count} rates in {Elapsed}ms",
            history.Count, stopwatch.ElapsedMilliseconds);

        return Ok(history.ToDtoList());
    }

    /// <summary>
    ///     Получить APR статистику по периодам
    /// </summary>
    /// <param name="symbol">Символ (обязательно, например: BTC-USDT)</param>
    /// <param name="exchanges">Список бирж (опционально, через запятую)</param>
    /// <returns>APR статистика по периодам для каждой биржи</returns>
    [HttpGet("apr-stats")]
    [ProducesResponseType(typeof(List<AprPeriodStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<AprPeriodStatsDto>>> GetAprStats(
        [FromQuery] string symbol,
        [FromQuery] string? exchanges)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol is required", nameof(symbol));

        _logger.LogDebug("GetAprStats: symbol={Symbol}, exchanges={Exchanges}", symbol, exchanges);

        var stopwatch = Stopwatch.StartNew();

        var exchangeList = exchanges.ParseExchanges();

        var stats = await _aprStatsService.GetAprStatsAsync(
            symbol,
            exchangeList?.Select(e => e.ToString()).ToList(),
            CancellationToken.None);

        stopwatch.Stop();
        _logger.LogInformation("GetAprStats completed: {Count} periods in {Elapsed}ms",
            stats.Count, stopwatch.ElapsedMilliseconds);

        return Ok(stats.ToDtoList());
    }
}