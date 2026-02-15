using Bybit.Net;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using CryptoExchange.Net.Objects;
using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ExchangeType = FundingMonitor.Core.Entities.ExchangeType;

namespace FundingMonitor.Infrastructure.ExchangeClients;

public class BybitApiClient : BaseExchangeApiClient
{
    private readonly BybitRestClient _bybitClient;

    public BybitApiClient(
        ILogger<BybitApiClient> logger,
        ISymbolNormalizer symbolNormalizer,
        IOptions<ExchangeOptions> bybitOptions)
        : base(logger, symbolNormalizer, bybitOptions)
    {
        _bybitClient = new BybitRestClient(bybitClientOptions =>
        {
            bybitClientOptions.Environment = BybitEnvironment.Live;
            bybitClientOptions.RequestTimeout = TimeSpan.FromSeconds(Options.TimeoutSeconds);
            bybitClientOptions.AutoTimestamp = true;
            bybitClientOptions.TimestampRecalculationInterval = TimeSpan.FromHours(1);
            bybitClientOptions.HttpVersion = new Version(2, 0);
            bybitClientOptions.HttpKeepAliveInterval = TimeSpan.FromSeconds(60);
            bybitClientOptions.RateLimiterEnabled = true;
            bybitClientOptions.RateLimitingBehaviour = RateLimitingBehaviour.Wait;
            bybitClientOptions.OutputOriginalData = false;
            bybitClientOptions.CachingEnabled = false;
        });
    }

    public override ExchangeType ExchangeType => ExchangeType.Bybit;

    public override async Task<List<CurrentFundingRate>> GetAllFundingRatesAsync(CancellationToken cancellationToken)
    {
        return await ExecuteWithMonitoringAsync(
            "GetAllFundingRates",
            async ct =>
            {
                var result = await _bybitClient.V5Api.ExchangeData.GetLinearInverseTickersAsync(
                    Category.Linear,
                    ct: ct);

                if (!result.Success)
                {
                    Logger.LogError("[Bybit] API Error: {Error}", result.Error?.Message);
                    throw new ExchangeApiException(ExchangeType, result.Error?.Message ?? "Unknown error");
                }

                var tickers = result.Data.List;
                var fundingRates = new List<CurrentFundingRate>(tickers.Length);

                foreach (var ticker in tickers)
                {
                    if (!IsValidSymbol(ticker.Symbol))
                        continue;

                    var fundingRate = CreateFundingRate(
                        ticker.Symbol,
                        ticker.MarkPrice,
                        ticker.IndexPrice,
                        ticker.FundingRate ?? 0,
                        ticker.NextFundingTime,
                        ticker.FundingInterval
                    );

                    fundingRates.Add(fundingRate);
                }

                Logger.LogInformation("[Bybit] Collected {Count} funding rates", fundingRates.Count);
                return fundingRates;
            },
            cancellationToken);
    }

    public override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteWithMonitoringAsync(
                "GetServerTime",
                async ct => await _bybitClient.V5Api.ExchangeData.GetServerTimeAsync(ct),
                cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
    }
}