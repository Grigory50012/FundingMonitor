using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.Application;

public sealed record HistoricalCollectionRequest(Guid EventId, ExchangeId Exchange, Symbol Symbol);
