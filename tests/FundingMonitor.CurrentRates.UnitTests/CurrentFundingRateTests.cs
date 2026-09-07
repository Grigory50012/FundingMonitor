using FundingMonitor.CurrentRates.Domain;
using FundingMonitor.SharedKernel;
using Xunit;

namespace FundingMonitor.CurrentRates.UnitTests;

public sealed class CurrentFundingRateTests
{
    [Theory]
    [InlineData(0.0001, 8, 10.95)]
    [InlineData(-0.0001, 8, -10.95)]
    [InlineData(0.0002, 4, 43.8)]
    public void Apr_ValidInterval_ReturnsAnnualizedPercentage(
        decimal fundingRate,
        int intervalHours,
        decimal expected)
    {
        var rate = CreateRate(fundingRate, intervalHours);

        Assert.Equal(expected, rate.Apr);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Apr_MissingOrInvalidInterval_ReturnsZero(int? intervalHours)
    {
        Assert.Equal(0m, CreateRate(intervalHours: intervalHours).Apr);
    }

    [Fact]
    public void Deviation_PositiveIndexPrice_ReturnsPercentage()
    {
        Assert.Equal(1m, CreateRate(markPrice: 101m, indexPrice: 100m).Deviation);
    }

    private static CurrentFundingRate CreateRate(
        decimal fundingRate = 0.0001m,
        int? intervalHours = 8,
        decimal markPrice = 100m,
        decimal indexPrice = 100m) =>
        new()
        {
            Exchange = ExchangeId.Parse("Binance"),
            Symbol = Symbol.Create("BTC-USDT"),
            BaseAsset = "BTC",
            QuoteAsset = "USDT",
            MarkPrice = markPrice,
            IndexPrice = indexPrice,
            FundingRate = fundingRate,
            FundingIntervalHours = intervalHours,
            NextFundingTime = new DateTimeOffset(2026, 9, 6, 16, 0, 0, TimeSpan.Zero),
            LastSeenOnUtc = new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero)
        };
}
