using Binance.Net;
using Binance.Net.Clients;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Options;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Exceptions;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ExchangeType = FundingMonitor.Core.Entities.ExchangeType;

namespace FundingMonitor.Infrastructure.ExchangeClients;

public class BinanceFundingRateClient : BaseExchangeFundingRateClient
{
    private readonly BinanceRestClient _binanceClient;
    private readonly ILogger<BinanceFundingRateClient> _logger;

    public BinanceFundingRateClient(
        ILogger<BinanceFundingRateClient> logger,
        ISymbolParser symbolParser,
        IOptions<ExchangeOptions> binanceOptions)
        : base(logger, symbolParser, binanceOptions)
    {
        _binanceClient = new BinanceRestClient(binanceClientOptions =>
        {
            binanceClientOptions.Environment = BinanceEnvironment.Live;
            binanceClientOptions.RequestTimeout = Options.RequestTimeout;
            binanceClientOptions.AutoTimestamp = true;
            binanceClientOptions.TimestampRecalculationInterval = TimeSpan.FromHours(1);
            binanceClientOptions.HttpVersion = new Version(2, 0);
            binanceClientOptions.HttpKeepAliveInterval = TimeSpan.FromSeconds(60);
            binanceClientOptions.RateLimiterEnabled = true;
            binanceClientOptions.RateLimitingBehaviour = RateLimitingBehaviour.Wait;
            binanceClientOptions.OutputOriginalData = false;
            binanceClientOptions.CachingEnabled = false;
        });

        _logger = logger;
    }

    public override ExchangeType ExchangeType => ExchangeType.Binance;

    public override async Task<List<CurrentFundingRate>> GetCurrentFundingRatesAsync(
        CancellationToken cancellationToken)
    {
        return await ExecuteApiCallWithTimeoutAsync(
            "Collection of current funding rates",
            async ct =>
            {
                var markPricesTask = _binanceClient.UsdFuturesApi.ExchangeData.GetMarkPricesAsync(ct);
                var intervalsTask = GetFundingIntervalsAsync(ct);

                await Task.WhenAll(markPricesTask, intervalsTask);

                var markPricesResult = markPricesTask.Result;
                var intervalsMap = intervalsTask.Result;

                if (!markPricesResult.Success)
                {
                    _logger.LogError("[Binance] API Error: {Error}", markPricesResult.Error?.Message);
                    throw new ExchangeApiException(ExchangeType, markPricesResult.Error?.Message ?? "Unknown error");
                }

                var rates = new List<CurrentFundingRate>(markPricesResult.Data.Length);

                foreach (var item in markPricesResult.Data)
                {
                    if (!IsValidSymbol(item.Symbol))
                        continue;
                    if (item.NextFundingTime < item.Timestamp)
                        continue;

                    // Получаем интервал из мапы (если есть)
                    intervalsMap.TryGetValue(item.Symbol, out var intervalHours);

                    rates.Add(CreateFundingRate(
                        item.Symbol,
                        item.MarkPrice,
                        item.IndexPrice,
                        item.FundingRate ?? 0,
                        item.NextFundingTime,
                        intervalHours > 0 ? intervalHours : null));
                }

                _logger.LogInformation("[Binance] Collected {Count} funding rates", rates.Count);
                return rates;
            },
            cancellationToken);
    }

    public override async Task<List<HistoricalFundingRate>> GetHistoricalFundingRatesAsync(
        string symbol,
        DateTime fromTime,
        DateTime toTime,
        int limit,
        CancellationToken cancellationToken)
    {
        return await ExecuteApiCallWithTimeoutAsync(
            $"Collection of funding rate history: {symbol}",
            async ct =>
            {
                var result = await _binanceClient.UsdFuturesApi.ExchangeData
                    .GetFundingRatesAsync(symbol, fromTime, toTime, limit, ct);

                if (!result.Success)
                {
                    _logger.LogError("[Binance] Historical API Error for {Symbol}: {Error}",
                        symbol, result.Error?.Message);
                    throw new ExchangeApiException(ExchangeType, result.Error?.Message ?? "Unknown error");
                }

                var rates = new List<HistoricalFundingRate>(result.Data.Length);

                foreach (var item in result.Data)
                {
                    if (!IsValidSymbol(item.Symbol))
                        continue;

                    rates.Add(CreateHistoricalFundingRate(
                        item.Symbol,
                        item.FundingRate,
                        item.FundingTime));
                }

                _logger.LogInformation("[Binance] Collected {Count} funding rates history", rates.Count);
                return rates;
            },
            cancellationToken);
    }

    public override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await ExecuteApiCallWithTimeoutAsync(
                "Ping",
                async ct => await _binanceClient.UsdFuturesApi.ExchangeData.PingAsync(ct),
                cancellationToken);

            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Получает информацию об интервалах финансирования для всех символов
    /// </summary>
    private async Task<Dictionary<string, int>> GetFundingIntervalsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _binanceClient.UsdFuturesApi.ExchangeData
                .GetFundingInfoAsync(cancellationToken);

            if (!result.Success)
            {
                _logger.LogDebug("[Binance] FundingInfo not available or empty");
                return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            }

            return result.Data
                .Where(info => IsValidSymbol(info.Symbol) && info.FundingIntervalHours > 0)
                .ToDictionary(
                    info => info.Symbol,
                    info => info.FundingIntervalHours,
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[Binance] Failed to fetch funding intervals");
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }
}