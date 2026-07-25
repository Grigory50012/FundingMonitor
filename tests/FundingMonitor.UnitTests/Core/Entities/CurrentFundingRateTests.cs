using System.Globalization;
using FundingMonitor.Core.Entities;
using Xunit;

namespace FundingMonitor.UnitTests.Core.Entities;

public sealed class CurrentFundingRateTests
{
    [Theory]
    [InlineData("0.0001", 8, "10.9500")]
    [InlineData("-0.0001", 8, "-10.9500")]
    [InlineData("0.0002", 4, "43.8000")]
    public void APR_ValidInterval_ReturnsAnnualizedPercentage(
        string rateText,
        int intervalHours,
        string expectedText)
    {
        var sut = CreateRate(
            fundingRate: decimal.Parse(rateText, CultureInfo.InvariantCulture),
            intervalHours: intervalHours);

        Assert.Equal(
            decimal.Parse(expectedText, CultureInfo.InvariantCulture),
            sut.APR);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void APR_MissingOrNonPositiveInterval_ReturnsZero(int? intervalHours)
    {
        var sut = CreateRate(fundingRate: 0.0001m, intervalHours: intervalHours);

        Assert.Equal(0m, sut.APR);
    }

    [Theory]
    [InlineData(8, 3)]
    [InlineData(4, 6)]
    [InlineData(24, 1)]
    [InlineData(5, 4)]
    public void NumberOfPaymentsPerDay_PositiveInterval_UsesIntegerDivision(
        int intervalHours,
        int expected)
    {
        var sut = CreateRate(intervalHours: intervalHours);

        Assert.Equal(expected, sut.NumberOfPaymentsPerDay);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void NumberOfPaymentsPerDay_MissingOrNonPositiveInterval_ReturnsDefault(int? intervalHours)
    {
        var sut = CreateRate(intervalHours: intervalHours);

        Assert.Equal(3, sut.NumberOfPaymentsPerDay);
    }

    [Theory]
    [InlineData("101", "100", "1.00")]
    [InlineData("99", "100", "-1.00")]
    public void Deviation_PositiveIndexPrice_ReturnsPercentage(
        string markText,
        string indexText,
        string expectedText)
    {
        var sut = CreateRate(
            markPrice: decimal.Parse(markText, CultureInfo.InvariantCulture),
            indexPrice: decimal.Parse(indexText, CultureInfo.InvariantCulture));

        Assert.Equal(
            decimal.Parse(expectedText, CultureInfo.InvariantCulture),
            sut.Deviation);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Deviation_NonPositiveIndexPrice_ReturnsZero(string indexText)
    {
        var sut = CreateRate(
            markPrice: 100m,
            indexPrice: decimal.Parse(indexText, CultureInfo.InvariantCulture));

        Assert.Equal(0m, sut.Deviation);
    }

    private static CurrentFundingRate CreateRate(
        decimal fundingRate = 0.0001m,
        int? intervalHours = 8,
        decimal markPrice = 100m,
        decimal indexPrice = 100m)
    {
        return new CurrentFundingRate
        {
            Exchange = ExchangeType.Binance,
            NormalizedSymbol = "BTC-USDT",
            BaseAsset = "BTC",
            QuoteAsset = "USDT",
            MarkPrice = markPrice,
            IndexPrice = indexPrice,
            FundingRate = fundingRate,
            FundingIntervalHours = intervalHours,
            NextFundingTime = new DateTime(2026, 7, 16, 16, 0, 0, DateTimeKind.Utc),
            LastSeenAt = new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc)
        };
    }
}
