using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Entities;
using Xunit;

namespace FundingMonitor.UnitTests.Core.Configuration;

public sealed class HistoricalDataCollectionOptionsTests
{
    [Fact]
    public void GetConcurrency_ExchangeOverrideExists_ReturnsOverride()
    {
        var options = new HistoricalDataCollectionOptions
        {
            DefaultConcurrencyPerExchange = 10,
            ExchangeConcurrency = new Dictionary<ExchangeType, int>
            {
                [ExchangeType.Binance] = 5
            }
        };

        Assert.Equal(5, options.GetConcurrency(ExchangeType.Binance));
    }

    [Fact]
    public void GetConcurrency_ExchangeOverrideMissing_ReturnsDefault()
    {
        var options = new HistoricalDataCollectionOptions
        {
            DefaultConcurrencyPerExchange = 10
        };

        Assert.Equal(10, options.GetConcurrency(ExchangeType.Bybit));
    }

    [Fact]
    public void GetConcurrency_ExchangeDisabled_ReturnsZero()
    {
        var options = new HistoricalDataCollectionOptions
        {
            DefaultConcurrencyPerExchange = 10,
            ExchangeConcurrency = new Dictionary<ExchangeType, int>
            {
                [ExchangeType.OKX] = 0
            }
        };

        Assert.Equal(0, options.GetConcurrency(ExchangeType.OKX));
    }
}
