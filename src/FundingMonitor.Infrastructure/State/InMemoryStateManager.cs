using System.Collections.Concurrent;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.State;
using FundingMonitor.Core.State;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.State;

public class InMemoryStateManager : IStateManager
{
    private readonly ILogger<InMemoryStateManager> _logger;
    private readonly ConcurrentDictionary<string, Dictionary<string, SymbolState>> _state = new();

    public InMemoryStateManager(ILogger<InMemoryStateManager> logger)
    {
        _logger = logger;
    }

    public Task<Dictionary<string, SymbolState>> GetExchangeStateAsync(ExchangeType exchange)
    {
        var key = exchange.ToString();
        _state.TryGetValue(key, out var state);
        return Task.FromResult(state ?? new Dictionary<string, SymbolState>());
    }

    public Task SaveExchangeStateAsync(ExchangeType exchange, Dictionary<string, SymbolState> state)
    {
        var key = exchange.ToString();
        _state[key] = state;
        _logger.LogDebug("Сохраненное состояние для {Exchange}: {Count} символов", exchange, state.Count);
        return Task.CompletedTask;
    }
}