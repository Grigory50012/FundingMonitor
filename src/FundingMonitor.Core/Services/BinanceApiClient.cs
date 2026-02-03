using FundingMonitor.Core.Enums;
using FundingMonitor.Core.Models;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Core.Services;

public class BinanceApiClient : BaseExchangeApiClient
{
    public override ExchangeType ExchangeType => ExchangeType.Binance;
    
    public BinanceApiClient(
        HttpClient httpClient,
        ILogger<BinanceApiClient> logger,
        SymbolNormalizer normalizer)
        : base(httpClient, logger, normalizer)
    {
    }
    
    public override async Task<List<NormalizedFundingRate>> GetAllFundingRatesAsync()
    {
        var response = await GetAsync<List<BinancePremiumIndexResponse>>("/fapi/v1/premiumIndex");
        
        return response.Select(r => new NormalizedFundingRate
        {
            Exchange = ExchangeType.Binance,
            OriginalSymbol = r.Symbol,
            NormalizedSymbol = SymbolNormalizer.Normalize(r.Symbol, ExchangeType),
            BaseAsset = SymbolNormalizer.Parse(r.Symbol, ExchangeType).Base,
            QuoteAsset = SymbolNormalizer.Parse(r.Symbol, ExchangeType).Quote,
            FundingRate = SafeParseDecimal(r.LastFundingRate),
            NextFundingTime = DateTimeOffset.FromUnixTimeMilliseconds(r.NextFundingTime).UtcDateTime,
            MarkPrice = SafeParseDecimal(r.MarkPrice),
            IndexPrice = SafeParseDecimal(r.IndexPrice),
            DataTime = DateTime.UtcNow,
            InstrumentType = "PERPETUAL"
        }).ToList();
    }
    
    public override async Task<NormalizedFundingRate?> GetFundingRateAsync(string symbol)
    {
        var response = await GetAsync<BinancePremiumIndexResponse>($"/fapi/v1/premiumIndex?symbol={symbol}");
        
        return new NormalizedFundingRate
        {
            Exchange = ExchangeType.Binance,
            OriginalSymbol = response.Symbol,
            NormalizedSymbol = SymbolNormalizer.Normalize(response.Symbol, ExchangeType),
            BaseAsset = SymbolNormalizer.Parse(response.Symbol, ExchangeType).Base,
            QuoteAsset = SymbolNormalizer.Parse(response.Symbol, ExchangeType).Quote,
            FundingRate = SafeParseDecimal(response.LastFundingRate),
            NextFundingTime = DateTimeOffset.FromUnixTimeMilliseconds(response.NextFundingTime).UtcDateTime,
            MarkPrice = SafeParseDecimal(response.MarkPrice),
            IndexPrice = SafeParseDecimal(response.IndexPrice),
            DataTime = DateTime.UtcNow,
            InstrumentType = "PERPETUAL"
        };
    }
    
    private class BinancePremiumIndexResponse
    {
        public string Symbol { get; set; } = string.Empty;
        public string MarkPrice { get; set; } = string.Empty;
        public string IndexPrice { get; set; } = string.Empty;
        public string LastFundingRate { get; set; } = string.Empty;
        public long NextFundingTime { get; set; }
    }
}