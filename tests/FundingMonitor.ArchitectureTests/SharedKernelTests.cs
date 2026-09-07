using FundingMonitor.SharedKernel;
using Xunit;

namespace FundingMonitor.ArchitectureTests;

public sealed class SharedKernelTests
{
    [Theory]
    [InlineData("Binance")]
    [InlineData("Bybit")]
    [InlineData("OKX")]
    public void ExchangeId_Parse_KnownExchange_RoundTrips(string value)
    {
        var exchange = ExchangeId.Parse(value);
        Assert.Equal(value, exchange.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Kraken")]
    public void ExchangeId_Parse_InvalidExchange_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => ExchangeId.Parse(value));
    }

    [Theory]
    [InlineData("BTC-USDT")]
    [InlineData("ETH-USDT")]
    public void Symbol_Create_NormalizedSymbol_RoundTrips(string value)
    {
        var symbol = Symbol.Create(value);
        Assert.Equal(value, symbol.Value);
    }
}