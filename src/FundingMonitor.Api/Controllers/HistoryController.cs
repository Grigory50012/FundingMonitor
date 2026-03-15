using FundingMonitor.Api.Models;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FundingMonitor.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HistoryController : ControllerBase
{
    private readonly ILogger<HistoryController> _logger;
    private readonly IHistoricalFundingRateRepository _repository;

    public HistoryController(
        IHistoricalFundingRateRepository repository,
        ILogger<HistoryController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    ///     Получить исторические ставки финансирования
    /// </summary>
    /// <param name="symbol">Символ (обязательно, например: BTC-USDT)</param>
    /// <param name="exchanges">Список бирж (опционально, через запятую)</param>
    /// <param name="from">Начальная дата (опционально, ISO формат)</param>
    /// <param name="to">Конечная дата (опционально, ISO формат)</param>
    /// <param name="limit">Максимальное количество записей (по умолчанию 100, макс 1000)</param>
    /// <returns>Исторические ставки</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<HistoricalFundingRate>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<HistoricalFundingRate>>> GetHistory(
        [FromQuery] string symbol,
        [FromQuery] string? exchanges,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? limit = 100)
    {
        if (string.IsNullOrWhiteSpace(symbol)) return BadRequest(new ApiErrorResponse { Error = "Symbol is required" });

        if (limit > 1000) limit = 1000;

        var exchangeList = ParseExchanges(exchanges);

        var history = await
            _repository.GetHistoryAsync(symbol, exchangeList, from, to, limit, CancellationToken.None);

        return Ok(history);
    }

    private List<ExchangeType>? ParseExchanges(string? exchanges)
    {
        if (string.IsNullOrWhiteSpace(exchanges))
            return null;

        return exchanges.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(e => Enum.Parse<ExchangeType>(e.Trim(), true))
            .ToList();
    }
}