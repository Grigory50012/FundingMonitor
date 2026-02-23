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

public class BinanceApiClient : BaseExchangeApiClient
{
    private readonly BinanceRestClient _binanceClient;

    public BinanceApiClient(
        ILogger<BinanceApiClient> logger,
        ISymbolNormalizer symbolNormalizer,
        IOptions<ExchangeOptions> binanceOptions)
        : base(logger, symbolNormalizer, binanceOptions)
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
    }

    public override ExchangeType ExchangeType => ExchangeType.Binance;

    public override async Task<List<CurrentFundingRate>> GetCurrentFundingRatesAsync(
        CancellationToken cancellationToken)
    {
        return await ExecuteWithMonitoringAsync(
            "Сбор текущих ставок финансирования",
            async ct =>
            {
                var result = await _binanceClient.UsdFuturesApi.ExchangeData.GetMarkPricesAsync(ct);

                if (!result.Success)
                {
                    Logger.LogError("[Binance] API Error: {Error}", result.Error?.Message);
                    throw new ExchangeApiException(ExchangeType, result.Error?.Message ?? "Unknown error");
                }

                var rates = new List<CurrentFundingRate>(result.Data.Length);

                foreach (var item in result.Data)
                {
                    if (!IsValidSymbol(item.Symbol)) // Только USDT
                        continue;
                    if (item.NextFundingTime < item.Timestamp) // Отсекаем неактивные или квартальные фьючерсы
                        continue;

                    rates.Add(CreateFundingRate(
                        item.Symbol,
                        item.MarkPrice,
                        item.IndexPrice,
                        item.FundingRate ?? 0,
                        item.NextFundingTime));
                }

                Logger.LogInformation("[Binance] Собрано {Count} ставок финансирования", rates.Count);
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
        return await ExecuteWithMonitoringAsync(
            $"Сбор истории ставок финансирования: {symbol}",
            async ct =>
            {
                var result = await _binanceClient.UsdFuturesApi.ExchangeData
                    .GetFundingRatesAsync(symbol, fromTime, toTime, limit, ct);

                if (!result.Success)
                {
                    Logger.LogError("[Binance] Historical API Error for {Symbol}: {Error}",
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

                Logger.LogInformation("[Binance] Собрано {Count} историй ставок финансирования", rates.Count);
                return rates;
            },
            cancellationToken);
    }

    public override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await ExecuteWithMonitoringAsync(
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
}