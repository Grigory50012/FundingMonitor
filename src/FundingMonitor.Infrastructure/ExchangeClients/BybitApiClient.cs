using System.Diagnostics;
using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.ExchangeClients;

public class BybitApiClient : BaseExchangeApiClient
{
    public override ExchangeType ExchangeType => ExchangeType.Bybit;
    private readonly ILogger _logger;
    private readonly ISymbolNormalizer _symbolNormalizer;
    
    public BybitApiClient(
        HttpClient httpClient, 
        ILogger<BybitApiClient> logger, 
        ISymbolNormalizer symbolNormalizer) 
        : base(httpClient, logger)
    {
        _logger = logger;
        _symbolNormalizer = symbolNormalizer;
    }
    
    public override async Task<List<NormalizedFundingRate>> GetAllFundingRatesAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await GetAsync<BybitTickersResponse>(
                "/v5/market/tickers?category=linear",  cancellationToken);
            
            var result = response.Result.List
                .Where(t => t.Symbol.EndsWith("USDT"))
                .Select(t => new NormalizedFundingRate
                {
                    Exchange = ExchangeType.Bybit,
                    NormalizedSymbol = _symbolNormalizer.Normalize(t.Symbol, ExchangeType),
                    MarkPrice = SafeParseDecimal(t.MarkPrice),
                    IndexPrice = SafeParseDecimal(t.IndexPrice),
                    FundingRate = SafeParseDecimal(t.FundingRate),
                    FundingIntervalHours = Convert.ToInt32(t.FundingIntervalHour),
                    NextFundingTime =
                        DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(t.NextFundingTime)).UtcDateTime,
                    LastCheck = DateTime.UtcNow,

                    IsActive = true,
                    BaseAsset = _symbolNormalizer.Parse(t.Symbol, ExchangeType).Base,
                    QuoteAsset = "USDT"
                })
                .ToList();
            
            stopwatch.Stop();
            _logger.LogInformation("[Bybit] собрано {Count} за {ElapsedMilliseconds} мс", result.Count, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[Bybit] Сбор не выполнен");
            throw new ExchangeApiException(ExchangeType.Bybit, $"Binance API error: {ex.Message}");
        }
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
        public string FundingIntervalHour { get; set; } = string.Empty;
        public string NextFundingTime { get; set; } = string.Empty;
    }
}