using FundingMonitor.Core.Enums;
using FundingMonitor.Core.Models;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Core.Services.Exchanges;

public class BinanceApiClient : BaseExchangeApiClient
{
    public override ExchangeType ExchangeType => ExchangeType.Binance;
    
    public BinanceApiClient(HttpClient httpClient, ILogger<BinanceApiClient> logger) : base(httpClient, logger)
    {
    }
    
    public override async Task<List<NormalizedFundingRate>> GetAllFundingRatesAsync()
    {
        var response = await GetAsync<List<BinancePremiumIndexResponse>>("/fapi/v1/premiumIndex");
        
        return response
            .Where(r => r.Symbol.EndsWith("USDT"))
            .Select(r => new NormalizedFundingRate
            {
                Exchange = ExchangeType.Binance,
                NormalizedSymbol = SymbolNormalizer.Normalize(r.Symbol, ExchangeType),
                MarkPrice = SafeParseDecimal(r.MarkPrice),
                IndexPrice = SafeParseDecimal(r.IndexPrice),
                FundingRate = SafeParseDecimal(r.LastFundingRate),
                // FundingIntervalHours = null; Binance не предоставляет Периодичность выплат.
                NextFundingTime = DateTimeOffset.FromUnixTimeMilliseconds(r.NextFundingTime).UtcDateTime,
                LastCheck = DateTime.UtcNow,
                
                IsActive = true,
                BaseAsset = SymbolNormalizer.Parse(r.Symbol, ExchangeType).Base,
                QuoteAsset = "USDT",
            }).ToList();
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