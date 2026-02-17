using Binance.Net;
using Binance.Net.Clients;
using CryptoExchange.Net.Objects;
using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Exceptions;
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
            binanceClientOptions.RequestTimeout = TimeSpan.FromSeconds(Options.TimeoutSeconds);
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
            "GetAllFundingRates",
            async ct =>
            {
                var result = await _binanceClient.UsdFuturesApi.ExchangeData.GetMarkPricesAsync(ct);

                if (!result.Success)
                {
                    Logger.LogError("[Binance] API Error: {Error}", result.Error?.Message);
                    throw new ExchangeApiException(ExchangeType, result.Error?.Message ?? "Unknown error");
                }

                var premiumIndices = result.Data;
                var fundingRates = new List<CurrentFundingRate>(premiumIndices.Length);

                foreach (var index in premiumIndices)
                {
                    if (!IsValidSymbol(index.Symbol)) // Только USDT
                        continue;
                    if (index.NextFundingTime < index.Timestamp) // Отсекаем неактивные или квартальные фьючерсы
                        continue;

                    var fundingRate = CreateFundingRate(
                        index.Symbol,
                        index.MarkPrice,
                        index.IndexPrice,
                        index.FundingRate ?? 0,
                        index.NextFundingTime
                    );

                    fundingRates.Add(fundingRate);
                }

                Logger.LogInformation("[Binance] Collected {Count} funding rates", fundingRates.Count);
                return fundingRates;
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