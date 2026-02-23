using FundingMonitor.Core.Entities;
using FundingMonitor.Core.State;

namespace FundingMonitor.Core.Interfaces.State;

public interface IStateManager
{
    Task<Dictionary<string, SymbolState>> GetExchangeStateAsync(ExchangeType exchange);
    Task SaveExchangeStateAsync(ExchangeType exchange, Dictionary<string, SymbolState> state);
}