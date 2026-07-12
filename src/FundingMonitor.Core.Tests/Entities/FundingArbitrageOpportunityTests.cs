using FundingMonitor.Core.Entities;
using Xunit;

namespace FundingMonitor.Core.Tests.Entities;

public class FundingArbitrageOpportunityTests
{
    [Fact]
    public void AprSpread_ReturnsAbsoluteDifferenceBetweenExchangeAprValues()
    {
        var opportunity = new FundingArbitrageOpportunity
        {
            Symbol = "BTC",
            ExchangeA = ExchangeType.Binance,
            ExchangeB = ExchangeType.Bybit,
            PriceA = 100_000m,
            PriceB = 99_900m,
            FundingRateA = 0.0001m,
            FundingRateB = -0.00005m,
            AprA = 10.95m,
            AprB = -5.475m,
            PaymentsA = 3,
            PaymentsB = 3
        };

        Assert.Equal(16.425m, opportunity.AprSpread);
    }
}
