using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Services;

/// <summary>
///     Детектор арбитража фандинга
/// </summary>
public interface IFundingArbitrageDetector
{
    Task<IReadOnlyList<FundingArbitrageOpportunity>> DetectAsync(CancellationToken cancellationToken);
}