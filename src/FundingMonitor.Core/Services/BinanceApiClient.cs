using System.Globalization;
using FundingMonitor.Core.Models;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Core.Services;

public class BinanceApiClient : BaseApiClient
{
    public override string ExchangeName => "Binance";
    
    public BinanceApiClient(HttpClient httpClient, ILogger<BinanceApiClient> logger) 
        : base(httpClient, logger)
    {
        _httpClient.BaseAddress = new Uri("https://fapi.binance.com");
    }
    
    public override async Task<List<TradingPairInfo>> GetAvailablePairsAsync()
    {
        try
        {
            var response = await GetAsync<BinanceExchangeInfoResponse>("/fapi/v1/exchangeInfo");
            
            return response.Symbols
                .Where(s => s.ContractType == "PERPETUAL" && s.Status == "TRADING")
                .Select(s => new TradingPairInfo
                {
                    Symbol = s.Symbol,
                    BaseAsset = s.BaseAsset,
                    QuoteAsset = s.QuoteAsset
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available pairs from Binance");
            return new List<TradingPairInfo>();
        }
    }
    
    public override async Task<FundingRateInfo> GetCurrentFundingRateAsync(string symbol)
    {
        var response = await GetAsync<BinancePremiumIndexResponse>($"/fapi/v1/premiumIndex?symbol={symbol}");
        
        return new FundingRateInfo
        {
            Symbol = symbol,
            Rate = SafeParseDecimal(response.LastFundingRate),
            NextFundingTime = DateTimeOffset.FromUnixTimeMilliseconds(response.NextFundingTime).UtcDateTime,
            MarkPrice = SafeParseDecimal(response.MarkPrice),
            IndexPrice = SafeParseDecimal(response.IndexPrice)
        };
    }
    
    private decimal SafeParseDecimal(string value, decimal defaultValue = 0m)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;
    
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
    
        _logger.LogWarning("Failed to parse decimal value: {Value}", value);
        return defaultValue;
    }
    
    public override async Task<List<FundingRateInfo>> GetFundingRateHistoryAsync(string symbol, int limit = 100)
    {
        var response = await GetAsync<List<BinanceFundingRateHistory>>(
            $"/fapi/v1/fundingRate?symbol={symbol}&limit={limit}");
        
        return response.Select(r => new FundingRateInfo
        {
            Symbol = symbol,
            Rate = decimal.Parse(r.FundingRate),
            NextFundingTime = DateTimeOffset.FromUnixTimeMilliseconds(r.FundingTime).UtcDateTime
        }).ToList();
    }
    
    public override async Task<decimal?> GetPredictedFundingRateAsync(string symbol)
    {
        // У Binance нет API для предсказанной ставки, возвращаем null
        return null;
    }
    
    // Классы для десериализации ответов Binance
    private class BinanceExchangeInfoResponse
    {
        public List<BinanceSymbol> Symbols { get; set; } = new();
    }
    
    private class BinanceSymbol
    {
        public string Symbol { get; set; } = string.Empty;
        public string BaseAsset { get; set; } = string.Empty;
        public string QuoteAsset { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
    }
    
    private class BinancePremiumIndexResponse
    {
        public string Symbol { get; set; } = string.Empty;
        public string MarkPrice { get; set; } = string.Empty;
        public string IndexPrice { get; set; } = string.Empty;
        public string LastFundingRate { get; set; } = string.Empty;
        public long NextFundingTime { get; set; }
    }
    
    private class BinanceFundingRateHistory
    {
        public string Symbol { get; set; } = string.Empty;
        public string FundingRate { get; set; } = string.Empty;
        public long FundingTime { get; set; }
    }
}