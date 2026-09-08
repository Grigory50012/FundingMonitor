using FundingMonitor.Arbitrage.Domain;
using FundingMonitor.SharedKernel;
using Xunit;

namespace FundingMonitor.Arbitrage.UnitTests;

public sealed class ArbitrageCalculatorTests
{
    [Fact]
    public void CalculatedProperties_PreserveLegacyFormulas()
    {
        var opportunity = new FundingArbitrageOpportunity
        {
            Symbol = Symbol.Create("BTC-USDT"),
            ExchangeA = ExchangeId.Parse("Binance"),
            ExchangeB = ExchangeId.Parse("Bybit"),
            PriceA = 100m,
            PriceB = 99m,
            FundingRateA = 0.001m,
            FundingRateB = -0.001m,
            AprA = 109.5m,
            AprB = -109.5m,
            PaymentsA = 3,
            PaymentsB = 3
        };

        Assert.Equal(1m, opportunity.PriceSpread);
        Assert.Equal((100m - 99m) / 99m * 100m, opportunity.PriceSpreadPercent);
        Assert.Equal(0.002m, opportunity.FundingRateSpread);
        Assert.Equal(219m, opportunity.AprSpread);
        Assert.Equal(ExchangeId.Parse("Binance"), opportunity.ShortExchange);
        Assert.Equal(ExchangeId.Parse("Bybit"), opportunity.LongExchange);
    }
}
