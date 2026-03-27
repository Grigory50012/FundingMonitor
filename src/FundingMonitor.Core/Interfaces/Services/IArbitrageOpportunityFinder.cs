using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Services;

/// <summary>
///     Сервис для поиска арбитражных возможностей между биржами
/// </summary>
public interface IArbitrageOpportunityFinder
{
    /// <summary>
    ///     Найти арбитражные возможности
    /// </summary>
    /// <param name="rates">Список текущих ставок финансирования</param>
    /// <returns>Список арбитражных возможностей</returns>
    List<ArbitrageOpportunity> FindOpportunities(List<CurrentFundingRate> rates);
}