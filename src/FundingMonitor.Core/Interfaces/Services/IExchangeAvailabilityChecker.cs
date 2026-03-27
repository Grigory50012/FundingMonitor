using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Services;

/// <summary>
///     Сервис для проверки доступности бирж
/// </summary>
public interface IExchangeAvailabilityChecker
{
    /// <summary>
    ///     Проверить доступность всех бирж
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Словарь: биржа → статус доступности</returns>
    Task<Dictionary<ExchangeType, bool>> CheckAllExchangesAsync(CancellationToken cancellationToken);
}