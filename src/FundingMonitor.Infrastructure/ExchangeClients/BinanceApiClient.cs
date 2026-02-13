using System.Diagnostics;
using Binance.Net;
using Binance.Net.Clients;
using CryptoExchange.Net.Objects;
using FundingMonitor.Application.Interfaces.Clients;
using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ExchangeType = FundingMonitor.Core.Entities.ExchangeType;

namespace FundingMonitor.Infrastructure.ExchangeClients;

public class BinanceApiClient : IExchangeApiClient
{
    private const string QuoteAsset = "USDT";
    private readonly BinanceRestClient _binanceClient;
    private readonly ILogger<BinanceApiClient> _logger;
    private readonly ISymbolNormalizer _symbolNormalizer;

    public BinanceApiClient(
        ILogger<BinanceApiClient> logger,
        ISymbolNormalizer symbolNormalizer,
        IOptions<ExchangeOptions> binanceOptions)
    {
        _logger = logger;
        _symbolNormalizer = symbolNormalizer;
        var options = binanceOptions.Value;

        _binanceClient = new BinanceRestClient(binanceClientOptions =>
        {
            binanceClientOptions.Environment = BinanceEnvironment.Live;
            binanceClientOptions.RequestTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            binanceClientOptions.AutoTimestamp = true;
            binanceClientOptions.TimestampRecalculationInterval = TimeSpan.FromHours(1);
            binanceClientOptions.HttpVersion = new Version(2, 0);
            binanceClientOptions.HttpKeepAliveInterval = TimeSpan.FromSeconds(60);
            binanceClientOptions.RateLimiterEnabled = true;
            binanceClientOptions.RateLimitingBehaviour = RateLimitingBehaviour.Wait;
            binanceClientOptions.OutputOriginalData = false;
            binanceClientOptions.CachingEnabled = false;
        });
    }

    public ExchangeType ExchangeType => ExchangeType.Binance;

    public async Task<List<CurrentFundingRate>> GetAllFundingRatesAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Используем USDⓈ-M Futures API для получения премиум индекса
            var result = await _binanceClient.UsdFuturesApi.ExchangeData.GetMarkPricesAsync(cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("[Binance] Ошибка: {Error}", result.Error?.Message);
                throw new ExchangeApiException(ExchangeType.Binance, result.Error?.Message ?? "Unknown error");
            }

            var premiumIndices = result.Data;
            var fundingRates = new List<CurrentFundingRate>(premiumIndices.Length);

            foreach (var index in premiumIndices)
            {
                if (!index.Symbol.EndsWith(QuoteAsset))
                    continue;

                var parsed = _symbolNormalizer.Parse(index.Symbol, ExchangeType);

                fundingRates.Add(new CurrentFundingRate
                {
                    Exchange = ExchangeType.Binance,
                    NormalizedSymbol = parsed.Base + "-" + parsed.Quote,
                    MarkPrice = index.MarkPrice,
                    IndexPrice = index.IndexPrice,
                    FundingRate = index.FundingRate ?? 0,
                    NextFundingTime = index.NextFundingTime,
                    LastCheck = DateTime.UtcNow,

                    IsActive = true,
                    BaseAsset = parsed.Base,
                    QuoteAsset = QuoteAsset
                });
            }

            stopwatch.Stop();
            _logger.LogInformation("[Binance] собрано {Count} за {ElapsedMilliseconds} мс",
                fundingRates.Count, stopwatch.ElapsedMilliseconds);

            return fundingRates;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[Binance] Сбор не выполнен");
            throw new ExchangeApiException(ExchangeType.Binance, $"Binance API error: {ex.Message}");
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _binanceClient.UsdFuturesApi.ExchangeData.PingAsync(cancellationToken);
            return result.Success;
        }
        catch
        {
            return false;
        }
    }
}