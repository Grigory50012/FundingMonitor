
namespace FundingMonitor.Core.CurrentRates.Interfaces;

/// <summary>
///     Детектор арбитража фандинга
/// </summary>
public interface IFundingArbitrageDetector
{
    Task<IReadOnlyList<FundingArbitrageOpportunity>> DetectAsync(CancellationToken cancellationToken);
}