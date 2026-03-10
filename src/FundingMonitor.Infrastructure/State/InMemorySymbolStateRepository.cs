using System.Collections.Concurrent;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.State;
using FundingMonitor.Core.State;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.State;

public class InMemorySymbolStateRepository : IStateManager
{
    private readonly ConcurrentDictionary<string, Dictionary<string, SymbolState>> _exchangeSymbolStates = new();
    private readonly ILogger<InMemorySymbolStateRepository> _logger;

    public InMemorySymbolStateRepository(ILogger<InMemorySymbolStateRepository> logger)
    {
        _logger = logger;
    }

    public Task<Dictionary<string, SymbolState>> GetExchangeStateAsync(ExchangeType exchange)
    {
        var key = exchange.ToString();
        _exchangeSymbolStates.TryGetValue(key, out var state);
        return Task.FromResult(state ?? new Dictionary<string, SymbolState>());
    }

    public Task SaveExchangeStateAsync(ExchangeType exchange, Dictionary<string, SymbolState> state)
    {
        var key = exchange.ToString();
        _exchangeSymbolStates[key] = state;
        _logger.LogDebug("Сохраненное состояние для {Exchange}: {Count} символов", exchange, state.Count);
        return Task.CompletedTask;
    }
}