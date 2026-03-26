using Bybit.Net;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using ExchangeType = FundingMonitor.Core.Entities.ExchangeType;

namespace FundingMonitor.Infrastructure.ExchangeClients;

public class BybitFundingRateClient : BaseExchangeFundingRateClient
{
    private readonly BybitRestClient _bybitClient;
    private readonly ILogger<BybitFundingRateClient> _logger;

    public BybitFundingRateClient(
        ILogger<BybitFundingRateClient> logger,
        ISymbolParser symbolParser)
        : base(logger, symbolParser)
    {
        _bybitClient = new BybitRestClient(options =>
        {
            options.Environment = BybitEnvironment.Live;
            options.AutoTimestamp = true;
            options.TimestampRecalculationInterval = TimeSpan.FromHours(1);
            options.HttpVersion = new Version(2, 0);
            options.HttpKeepAliveInterval = TimeSpan.FromSeconds(15);
            options.RateLimiterEnabled = true;
            options.OutputOriginalData = true;
            options.CachingEnabled = false;
        });

        _logger = logger;
    }

    public override ExchangeType ExchangeType => ExchangeType.Bybit;

    public override async Task<List<CurrentFundingRate>> GetCurrentFundingRatesAsync(
        CancellationToken cancellationToken)
    {
        return await ExecuteApiCallAsync(
            "Collection of current funding rates",
            async ct =>
            {
                var result = await _bybitClient.V5Api.ExchangeData.GetLinearInverseTickersAsync(
                    Category.Linear, ct: ct);

                if (!result.Success)
                {
                    _logger.LogError("[Bybit] API Error: {Error}", result.Error?.Message);
                    return [];
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

                _logger.LogDebug("[Bybit] Collected {Count} funding rates", rates.Count);
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
        return await ExecuteApiCallAsync(
            $"Collection of funding rate history: {symbol}",
            async ct =>
            {
                symbol = ConvertToBybitSymbol(symbol);

                var result = await _bybitClient.V5Api.ExchangeData
                    .GetFundingRateHistoryAsync(Category.Linear, symbol, fromTime, toTime, limit, ct);

                if (!result.Success)
                {
                    _logger.LogError("[Bybit] Historical API Error for {Symbol}: {Error}",
                        symbol, result.Error?.Message);
                    return [];
                }

                var rates = new List<HistoricalFundingRate>(result.Data.List.Length);

                foreach (var item in result.Data.List)
                {
                    rates.Add(CreateHistoricalFundingRate(
                        item.Symbol,
                        item.FundingRate,
                        item.Timestamp));
                }

                _logger.LogDebug("[Bybit] Collected {Count} funding rates history", rates.Count);
                return rates;
            },
            cancellationToken);
    }

    public override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteApiCallAsync(
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

    /// <summary>
    ///     Конвертирует символ из формата "BTC-USDT" в "BTCUSDT"
    /// </summary>
    private static string ConvertToBybitSymbol(string symbol)
    {
        return symbol.Replace("-", "");
    }
}