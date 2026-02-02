using System.Globalization;
using FundingMonitor.Core.Models;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Core.Services;

public class BybitApiClient : BaseApiClient
{
    public override string ExchangeName => "Bybit";
    
    public BybitApiClient(HttpClient httpClient, ILogger<BybitApiClient> logger) 
        : base(httpClient, logger)
    {
        _httpClient.BaseAddress = new Uri("https://api.bybit.com");
    }
    
    public override async Task<List<TradingPairInfo>> GetAvailablePairsAsync()
    {
        try
        {
            var response = await GetAsync<BybitTickersResponse>("/v5/market/tickers?category=linear");
            
            return response.Result.List
                .Where(t => t.Symbol.EndsWith("USDT"))
                .Select(t => new TradingPairInfo
                {
                    Symbol = t.Symbol,
                    BaseAsset = ParseBaseAsset(t.Symbol),
                    QuoteAsset = "USDT",
                    Price = SafeParseDecimal(t.LastPrice),
                    MarkPrice = SafeParseDecimal(t.MarkPrice),
                    IndexPrice = SafeParseDecimal(t.IndexPrice),
                    FundingRate = SafeParseDecimal(t.FundingRate),
                    NextFundingTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(t.NextFundingTime)).UtcDateTime,
                    PredictedFundingRate = decimal.TryParse(t.PredictedFundingRate, 
                        NumberStyles.Any, CultureInfo.InvariantCulture, out var predicted) ? predicted : null
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available pairs from Bybit");
            return new List<TradingPairInfo>();
        }
    }
    
    public override async Task<FundingRateInfo> GetCurrentFundingRateAsync(string symbol)
    {
        // У Bybit удобнее получить все пары сразу
        var pairs = await GetAvailablePairsAsync();
        var pair = pairs.FirstOrDefault(p => p.Symbol == symbol);
        
        if (pair == null)
            throw new ArgumentException($"Symbol {symbol} not found on Bybit");
        
        return new FundingRateInfo
        {
            Symbol = symbol,
            Rate = pair.FundingRate,
            NextFundingTime = pair.NextFundingTime ?? DateTime.UtcNow.AddHours(8),
            PredictedRate = pair.PredictedFundingRate,
            MarkPrice = pair.MarkPrice,
            IndexPrice = pair.IndexPrice
        };
    }
    
    public override async Task<List<FundingRateInfo>> GetFundingRateHistoryAsync(string symbol, int limit = 100)
    {
        var response = await GetAsync<BybitFundingHistoryResponse>(
            $"/v5/market/funding/history?category=linear&symbol={symbol}&limit={limit}");
        
        return response.Result.List.Select(r => new FundingRateInfo
        {
            Symbol = symbol,
            Rate = decimal.Parse(r.FundingRate),
            NextFundingTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(r.FundingRateTimestamp)).UtcDateTime
        }).ToList();
    }
    
    public override async Task<decimal?> GetPredictedFundingRateAsync(string symbol)
    {
        var pairs = await GetAvailablePairsAsync();
        var pair = pairs.FirstOrDefault(p => p.Symbol == symbol);
        return pair?.PredictedFundingRate;
    }
    
    private static string ParseBaseAsset(string symbol)
    {
        // BTCUSDT → BTC
        return symbol.EndsWith("USDT") ? symbol[..^4] : symbol;
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

    // Классы для десериализации ответов Bybit
    private class BybitTickersResponse
    {
        public int RetCode { get; set; }
        public string RetMsg { get; set; } = string.Empty;
        public BybitTickersResult Result { get; set; } = null!;
    }

    private class BybitTickersResult
    {
        public string Category { get; set; } = string.Empty;
        public List<BybitTicker> List { get; set; } = new();
    }

    private class BybitTicker
    {
        public string Symbol { get; set; } = string.Empty;
        public string LastPrice { get; set; } = string.Empty;
        public string IndexPrice { get; set; } = string.Empty;
        public string MarkPrice { get; set; } = string.Empty;
        public string FundingRate { get; set; } = string.Empty;
        public string FundingIntervalHour { get; set; } = string.Empty;
        public string NextFundingTime { get; set; } = string.Empty;
        public string PredictedFundingRate { get; set; } = string.Empty;
    }

    private class BybitFundingHistoryResponse
    {
        public int RetCode { get; set; }
        public string RetMsg { get; set; } = string.Empty;
        public BybitFundingHistoryResult Result { get; set; } = null!;
    }

    private class BybitFundingHistoryResult
    {
        public List<BybitFundingHistory> List { get; set; } = new();
    }

    private class BybitFundingHistory
    {
        public string Symbol { get; set; } = string.Empty;
        public string FundingRate { get; set; } = string.Empty;
        public string FundingRateTimestamp { get; set; } = string.Empty;
    }
}