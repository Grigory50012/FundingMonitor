using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.IntegrationTests.Infrastructure;

internal static class TestEvents
{
    public static FundingRateChangedIntegrationEvent BtcChanged => new(
        Guid.Parse("30000000-0000-0000-0000-000000000001"),
        new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero),
        ExchangeId.Parse("Binance"),
        Symbol.Create("BTC-USDT"),
        new DateTimeOffset(2026, 9, 8, 16, 0, 0, TimeSpan.Zero));
}

internal static class TestHistory
{
    public static HistoricalRatePoint BtcPoint => new(
        ExchangeId.Parse("Binance"),
        Symbol.Create("BTC-USDT"),
        0.0001m,
        new DateTimeOffset(2026, 9, 8, 8, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero));
}
