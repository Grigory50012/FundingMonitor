using Bybit.Net;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Options;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Exceptions;
using FundingMonitor.Core.Interfaces.Services;
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
            bybitClientOptions.RequestTimeout = Options.RequestTimeout;
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

    public override async Task<List<CurrentFundingRate>> GetCurrentFundingRatesAsync(
        CancellationToken cancellationToken)
    {
        return await ExecuteWithMonitoringAsync(
            "GetCurrentFundingRates",
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

                var rates = new List<CurrentFundingRate>(result.Data.List.Length);

                foreach (var item in result.Data.List)
                {
                    if (!IsValidSymbol(item.Symbol))
                        continue;

                    rates.Add(CreateFundingRate(
                        item.Symbol,
                        item.MarkPrice,
                        item.IndexPrice,
                        item.FundingRate ?? 0,
                        item.NextFundingTime,
                        item.FundingInterval));
                }

                Logger.LogInformation("[Bybit] Собрано {Count} ставок финансирования", rates.Count);
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
            $"GetHistoricalFundingRates: {symbol}",
            async ct =>
            {
                var result = await _bybitClient.V5Api.ExchangeData
                    .GetFundingRateHistoryAsync(Category.Linear, symbol, fromTime, toTime, limit, ct);

                if (!result.Success)
                {
                    Logger.LogError("[Bybit] Historical API Error for {Symbol}: {Error}",
                        symbol, result.Error?.Message);
                    throw new ExchangeApiException(ExchangeType, result.Error?.Message ?? "Unknown error");
                }

                var rates = new List<HistoricalFundingRate>(result.Data.List.Length);

                foreach (var item in result.Data.List)
                {
                    if (!IsValidSymbol(item.Symbol))
                        continue;

                    rates.Add(CreateHistoricalFundingRate(
                        item.Symbol,
                        item.FundingRate,
                        item.Timestamp));
                }

                Logger.LogInformation("[Bybit] Собрано {Count} историй ставок финансирования", rates.Count);
                return rates;
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