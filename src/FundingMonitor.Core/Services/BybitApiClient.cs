using FundingMonitor.Core.Enums;
using FundingMonitor.Core.Models;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Core.Services;

public class BybitApiClient : BaseExchangeApiClient
{
    public override ExchangeType ExchangeType => ExchangeType.Bybit;
    
    public BybitApiClient(
        HttpClient httpClient,
        ILogger<BybitApiClient> logger,
        SymbolNormalizer normalizer)
        : base(httpClient, logger, normalizer)
    {
    }
    
    public override async Task<List<NormalizedFundingRate>> GetAllFundingRatesAsync()
    {
        var response = await GetAsync<BybitTickersResponse>("/v5/market/tickers?category=linear");
        
        return response.Result.List
            .Where(t => t.Symbol.EndsWith("USDT"))
            .Select(t => new NormalizedFundingRate
            {
                Exchange = ExchangeType.Bybit,
                OriginalSymbol = t.Symbol,
                NormalizedSymbol = SymbolNormalizer.Normalize(t.Symbol, ExchangeType),
                BaseAsset = SymbolNormalizer.Parse(t.Symbol, ExchangeType).Base,
                QuoteAsset = SymbolNormalizer.Parse(t.Symbol, ExchangeType).Quote,
                FundingRate = SafeParseDecimal(t.FundingRate),
                NextFundingTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(t.NextFundingTime)).UtcDateTime,
                PredictedNextRate = SafeParseDecimal(t.PredictedFundingRate),
                MarkPrice = SafeParseDecimal(t.MarkPrice),
                IndexPrice = SafeParseDecimal(t.IndexPrice),
                DataTime = DateTime.UtcNow,
                InstrumentType = "PERPETUAL"
            }).ToList();
    }
    
    public override async Task<NormalizedFundingRate?> GetFundingRateAsync(string symbol)
    {
        var rates = await GetAllFundingRatesAsync();
        return rates.FirstOrDefault(r => r.OriginalSymbol == symbol || r.NormalizedSymbol == symbol);
    }
    
    private class BybitTickersResponse
    {
        public BybitTickersResult Result { get; set; } = null!;
    }
    
    private class BybitTickersResult
    {
        public List<BybitTicker> List { get; set; } = new();
    }
    
    private class BybitTicker
    {
        public string Symbol { get; set; } = string.Empty;
        public string MarkPrice { get; set; } = string.Empty;
        public string IndexPrice { get; set; } = string.Empty;
        public string FundingRate { get; set; } = string.Empty;
        public string PredictedFundingRate { get; set; } = string.Empty;
        public string NextFundingTime { get; set; } = string.Empty;
    }
}