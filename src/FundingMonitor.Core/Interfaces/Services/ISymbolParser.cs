using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Services;

public interface ISymbolParser
{
    (string Base, string Quote) Parse(string symbol, ExchangeType exchange);
}