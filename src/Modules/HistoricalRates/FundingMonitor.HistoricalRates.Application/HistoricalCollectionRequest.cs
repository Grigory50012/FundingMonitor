using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.Application;

public sealed record HistoricalCollectionRequest(ExchangeId Exchange, Symbol Symbol);
