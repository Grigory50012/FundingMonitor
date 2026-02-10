using FundingMonitor.Core.Entities;

namespace FundingMonitor.Application.Interfaces.Services;

public interface ISymbolNormalizer
{
    string Normalize(string symbol, ExchangeType exchange);
    (string Base, string Quote) Parse(string symbol, ExchangeType exchange);
}