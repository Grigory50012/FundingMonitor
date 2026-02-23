using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Services;

public interface ISymbolNormalizer
{
    (string Base, string Quote) Parse(string symbol, ExchangeType exchange);
}