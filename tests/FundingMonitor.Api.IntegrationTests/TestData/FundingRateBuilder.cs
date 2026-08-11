
namespace FundingMonitor.Api.IntegrationTests.TestData;

internal sealed class FundingRateBuilder
{
    private string _exchange = "Binance";
    private string _normalizedSymbol = "BTC-USDT";
    private string _baseAsset = "BTC";
    private string _quoteAsset = "USDT";
    private decimal? _markPrice = 65_000.50m;
    private decimal? _indexPrice = 64_990m;
    private decimal _fundingRate = 0.0001m;
    private int _fundingIntervalHours = 8;
    private DateTime? _nextFundingTime = new(2026, 7, 16, 16, 0, 0, DateTimeKind.Utc);
    private DateTime _lastSeenAt = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);
    private bool _isActive = true;

    public FundingRateBuilder WithExchange(string exchange)
    {
        _exchange = exchange;
        return this;
    }

    public FundingRateBuilder WithSymbol(
        string normalizedSymbol,
        string baseAsset,
        string quoteAsset = "USDT")
    {
        _normalizedSymbol = normalizedSymbol;
        _baseAsset = baseAsset;
        _quoteAsset = quoteAsset;
        return this;
    }

    public FundingRateBuilder WithFunding(
        decimal markPrice,
        decimal fundingRate,
        int intervalHours = 8)
    {
        _markPrice = markPrice;
        _fundingRate = fundingRate;
        _fundingIntervalHours = intervalHours;
        return this;
    }

    public FundingRateBuilder Inactive()
    {
        _isActive = false;
        return this;
    }

    public CurrentFundingRateDb Build() =>
        new()
        {
            Exchange = _exchange,
            NormalizedSymbol = _normalizedSymbol,
            BaseAsset = _baseAsset,
            QuoteAsset = _quoteAsset,
            MarkPrice = _markPrice,
            IndexPrice = _indexPrice,
            FundingRate = _fundingRate,
            FundingIntervalHours = _fundingIntervalHours,
            NextFundingTime = _nextFundingTime,
            LastSeenAt = _lastSeenAt,
            IsActive = _isActive
        };
}
