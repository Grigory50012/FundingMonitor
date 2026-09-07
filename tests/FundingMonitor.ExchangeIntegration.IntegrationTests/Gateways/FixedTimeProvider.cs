namespace FundingMonitor.ExchangeIntegration.IntegrationTests.Gateways;

internal sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => value;
}
