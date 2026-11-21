using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace FundingMonitor.HistoricalRates.Application;

public sealed class AprStatsCacheInvalidation(IMemoryCache cache)
{
    private readonly object gate = new();

    public IChangeToken GetToken(Symbol symbol)
    {
        lock (gate)
        {
            var key = $"apr-token:{symbol.Value}";
            if (!cache.TryGetValue(key, out ExpirationSource? source))
            {
                source = new ExpirationSource();
                cache.Set(key, source, new MemoryCacheEntryOptions()
                    .SetSize(1)
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                    .RegisterPostEvictionCallback((_, value, _, _) =>
                        ((ExpirationSource)value!).Invalidate()));
                // If the size limit rejects the token, do not cache dependent results.
                if (!cache.TryGetValue(key, out ExpirationSource? stored) || !ReferenceEquals(source, stored))
                    source.Invalidate();
            }
            return source!.Token;
        }
    }

    public void Invalidate(Symbol symbol)
    {
        lock (gate)
        {
            var key = $"apr-token:{symbol.Value}";
            if (cache.TryGetValue(key, out ExpirationSource? source))
            {
                cache.Remove(key);
                source!.Invalidate();
            }
        }
    }

    private sealed class ExpirationSource
    {
        private readonly CancellationTokenSource source = new();
        private readonly object gate = new();
        private bool invalidated;
        public IChangeToken Token { get; }

        public ExpirationSource() => Token = new CancellationChangeToken(source.Token);

        public void Invalidate()
        {
            lock (gate)
            {
                if (invalidated) return;
                invalidated = true;
                source.Cancel();
                source.Dispose();
            }
        }
    }
}
