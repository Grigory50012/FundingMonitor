using System.Text.Json.Serialization;

namespace FundingMonitor.CurrentRates.Presentation.Dtos;

public sealed record FundingRateDto(
    string Exchange,
    string Symbol,
    decimal MarkPrice,
    decimal FundingRate,
    decimal APR,
    int NumberOfPaymentsPerDay,
    [property: JsonRequired] DateTime? NextFundingTime,
    string ExchangeUrl);
