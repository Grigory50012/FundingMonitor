namespace FundingMonitor.Application.DTOs;

public class FundingRateDto
{
    public string Exchange { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal? MarkPrice { get; set; }
    public decimal? IndexPrice { get; set; }
    public decimal FundingRate { get; set; }
    public int FundingIntervalHours { get; set; }
    public DateTime NextFundingTime { get; set; }
    public decimal? PredictedRate { get; set; }
    public string BaseAsset { get; set; } = string.Empty;
    public string QuoteAsset { get; set; } = string.Empty;
}