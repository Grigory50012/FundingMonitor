using Binance.Net.Clients;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Exceptions;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using ExchangeType = FundingMonitor.Core.Entities.ExchangeType;

namespace FundingMonitor.Infrastructure.ExchangeClients;

public class BinanceFundingRateClient : BaseExchangeFundingRateClient
{
    private readonly BinanceRestClient _binanceClient;
    private readonly ILogger<BinanceFundingRateClient> _logger;

    public BinanceFundingRateClient(
        ILogger<BinanceFundingRateClient> logger,
        ISymbolService symbolService,
        BinanceRestClient binanceClient)
        : base(logger, symbolService)
    {
        _binanceClient = binanceClient;
        _logger = logger;
    }

    public override ExchangeType ExchangeType => ExchangeType.Binance;

    public override async Task<List<CurrentFundingRate>> GetCurrentFundingRatesAsync(
        CancellationToken cancellationToken)
    {
        return await ExecuteApiCallAsync(
            "Collection of current funding rates",
            async ct =>
            {
                var markPricesTask = _binanceClient.UsdFuturesApi.ExchangeData.GetMarkPricesAsync(ct);
                var intervalsTask = GetFundingIntervalsAsync(ct);

                await Task.WhenAll(markPricesTask, intervalsTask);

                var markPricesResult = await markPricesTask;
                var intervalsMap = await intervalsTask;

                if (!markPricesResult.Success)
                {
                    var errorMessage = markPricesResult.Error?.Message ?? "Unknown error";
                    _logger.LogError("[Binance] API Error: {Error}", errorMessage);
                    throw new ExchangeApiException(
                        ExchangeType.Binance,
                        $"Binance API error: {errorMessage}");
                }

                var rates = new List<CurrentFundingRate>(markPricesResult.Data.Length);

                foreach (var item in markPricesResult.Data)
                {
                    if (!IsValidSymbol(item.Symbol))
                        continue;
                    if (item.NextFundingTime < item.Timestamp)
                        continue;
                    if (item is { EstimatedSettlePrice: 0, FundingRate: 0 })
                        continue;
                    if (!intervalsMap.TryGetValue(item.Symbol, out var intervalHours))
                        continue;

                    rates.Add(CreateFundingRate(
                        item.Symbol,
                        item.MarkPrice,
                        item.IndexPrice,
                        item.FundingRate ?? 0,
                        item.NextFundingTime,
                        intervalHours));
                }

                _logger.LogDebug("[Binance] Collected {Count} funding rates", rates.Count);
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
                symbol = ConvertToExchangeSymbol(symbol);

                var result = await _binanceClient.UsdFuturesApi.ExchangeData
                    .GetFundingRatesAsync(symbol, fromTime, toTime, limit, ct);

                if (!result.Success)
                {
                    var errorMessage = result.Error?.Message ?? "Unknown error";
                    var isForbidden = errorMessage.Contains("403", StringComparison.OrdinalIgnoreCase)
                                      || errorMessage.Contains("Forbidden", StringComparison.OrdinalIgnoreCase);

                    if (isForbidden)
                        _logger.LogWarning(
                            "[Binance] Access denied (403) for {Symbol}: {Error}. This may be due to rate limiting, geoblocking, or invalid symbol.",
                            symbol, errorMessage);
                    else
                        _logger.LogError("[Binance] Historical API Error for {Symbol}: {Error}",
                            symbol, errorMessage);

                    throw new ExchangeApiException(
                        ExchangeType.Binance,
                        $"Binance API error: {errorMessage}",
                        result.Error?.Code ?? 0);
                }

                var rates = new List<HistoricalFundingRate>(result.Data.Length);

                foreach (var item in result.Data)
                {
                    rates.Add(CreateHistoricalFundingRate(
                        item.Symbol,
                        item.FundingRate,
                        item.FundingTime));
                }

                _logger.LogDebug("[Binance] Collected {Count} funding rates history", rates.Count);
                return rates;
            },
            cancellationToken);
    }

    public override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteApiCallAsync(
                "Ping",
                async ct => await _binanceClient.UsdFuturesApi.ExchangeData.PingAsync(ct),
                cancellationToken);
        }
        catch
        {
            return false;
        }
    }

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