namespace FundingMonitor.HistoricalRates.Presentation.Dtos;

public sealed record HistoricalFundingRateDto(
    string Exchange,
    string Symbol,
    decimal FundingRate,
    DateTime FundingTime);
