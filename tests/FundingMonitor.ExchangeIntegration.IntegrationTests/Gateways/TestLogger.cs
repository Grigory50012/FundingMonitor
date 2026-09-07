using Microsoft.Extensions.Logging;

namespace FundingMonitor.ExchangeIntegration.IntegrationTests.Gateways;

internal sealed class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Console.WriteLine($"{logLevel}: {formatter(state, exception)} | {exception}");
}
