namespace FundingMonitor.Api.Models.Dtos;

public class HistoricalFundingRateDto
{
    public string Exchange { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal FundingRate { get; set; }
    public DateTime FundingTime { get; set; }
}