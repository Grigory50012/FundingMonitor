using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace FundingMonitor.Api.Controllers;

/// <summary>
///     Контроллер для проверки статуса бирж
/// </summary>
[ApiController]
[Route("api/v1/exchanges")]
public class ExchangesController : ControllerBase
{
    private readonly IExchangeAvailabilityChecker _checker;
    private readonly ILogger<ExchangesController> _logger;

    public ExchangesController(
        IExchangeAvailabilityChecker checker,
        ILogger<ExchangesController> logger)
    {
        _checker = checker;
        _logger = logger;
    }

    /// <summary>
    ///     Проверить доступность бирж
    /// </summary>
    /// <remarks>
    ///     Возвращает статус каждой биржи (true = доступна, false = недоступна)
    /// </remarks>
    /// <returns>Статус каждой биржи</returns>
    /// <response code="200">Успешная проверка</response>
    /// <response code="500">Ошибка при проверке</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(Dictionary<ExchangeType, bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Dictionary<ExchangeType, bool>>> GetHealth(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking exchanges health");

        try
        {
            var health = await _checker.CheckAllExchangesAsync(cancellationToken);

            _logger.LogInformation(
                "Exchanges health: {Health}",
                string.Join(", ", health.Select(h => $"{h.Key}={h.Value}")));

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check exchanges health");
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }
}