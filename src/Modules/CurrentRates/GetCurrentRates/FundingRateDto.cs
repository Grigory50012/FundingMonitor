using System.Text.Json.Serialization;

namespace FundingMonitor.CurrentRates.GetCurrentRates;

public sealed record FundingRateDto(
    string Exchange,
    string Symbol,
    decimal MarkPrice,
    decimal FundingRate,
    decimal APR,
    int NumberOfPaymentsPerDay,
    [property: JsonRequired] DateTime? NextFundingTime,
    string ExchangeUrl);
