using System.Diagnostics;
using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.ExchangeClients;

public class BinanceApiClient : BaseExchangeApiClient
{
    public override ExchangeType ExchangeType => ExchangeType.Binance;
    private readonly ILogger _logger;
    private readonly ISymbolNormalizer _symbolNormalizer;
    
    public BinanceApiClient(
        HttpClient httpClient, 
        ILogger<BinanceApiClient> logger, 
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
            var response = await GetAsync<List<BinancePremiumIndexResponse>>(
                "/fapi/v1/premiumIndex", cancellationToken);
            
            var result = response
                .Where(r => r.Symbol.EndsWith("USDT"))
                .Select(r => new NormalizedFundingRate
                {
                    Exchange = ExchangeType.Binance,
                    NormalizedSymbol = _symbolNormalizer.Normalize(r.Symbol, ExchangeType),
                    MarkPrice = SafeParseDecimal(r.MarkPrice),
                    IndexPrice = SafeParseDecimal(r.IndexPrice),
                    FundingRate = SafeParseDecimal(r.LastFundingRate),
                    NextFundingTime = DateTimeOffset.FromUnixTimeMilliseconds(r.NextFundingTime).UtcDateTime,
                    LastCheck = DateTime.UtcNow,

                    IsActive = true,
                    BaseAsset = _symbolNormalizer.Parse(r.Symbol, ExchangeType).Base,
                    QuoteAsset = "USDT"
                })
                .ToList();
            
            stopwatch.Stop();
            _logger.LogInformation("[Binance] собрано {Count} за {ElapsedMilliseconds} мс", result.Count, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[Binance] Сбор не выполнен");
            throw new ExchangeApiException(ExchangeType.Binance, $"Binance API error: {ex.Message}");
        }
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