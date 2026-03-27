using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Events;

namespace FundingMonitor.Core.Interfaces.Services;

/// <summary>
///     Сервис для детектирования изменений в ставках финансирования
/// </summary>
public interface IFundingRateChangeDetector
{
    /// <summary>
    ///     Детектирует изменения в ставках финансирования
    /// </summary>
    /// <param name="exchange">Биржа</param>
    /// <param name="previous">Предыдущее состояние ставок</param>
    /// <param name="current">Текущее состояние ставок</param>
    /// <returns>Список событий об изменениях</returns>
    List<FundingRateChangedEvent> DetectChanges(
        ExchangeType exchange,
        Dictionary<string, CurrentFundingRate> previous,
        List<CurrentFundingRate> current);
}