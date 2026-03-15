namespace FundingMonitor.Api.Models.Dtos;

public class FundingRateDto
{
    public string Exchange { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal MarkPrice { get; set; }
    public decimal FundingRate { get; set; }
    public decimal APR { get; set; }
    public int NumberOfPaymentsPerDay { get; set; }
    public DateTime? NextFundingTime { get; set; }
    public bool IsActive { get; set; }
}