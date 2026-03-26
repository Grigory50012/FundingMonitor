using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using OKX.Net.Clients;
using OKX.Net.Enums;
using OKX.Net.Objects.Public;

namespace FundingMonitor.Infrastructure.ExchangeClients;

public class OkxFundingRateClient : BaseExchangeFundingRateClient
{
    private readonly ILogger<OkxFundingRateClient> _logger;
    private readonly OKXRestClient _okxClient;

    public OkxFundingRateClient(
        ILogger<OkxFundingRateClient> logger,
        ISymbolService symbolService,
        OKXRestClient okxClient)
        : base(logger, symbolService)
    {
        _okxClient = okxClient;
        _logger = logger;
    }

    public override ExchangeType ExchangeType => ExchangeType.OKX;

    public override async Task<List<CurrentFundingRate>> GetCurrentFundingRatesAsync(
        CancellationToken cancellationToken)
    {
        return await ExecuteApiCallAsync(
            "Collection of current funding rates",
            async ct =>
            {
                // 1. Получаем список всех SWAP инструментов
                var swapInstruments = await GetAllInstrumentsAsync(ct);

                // 2. Получаем mark prices для всех инструментов
                var markPrices = await GetMarkPricesMapAsync(ct);

                // 3. Получаем index prices для всех инструментов
                var indexPrices = await GetIndexPricesMapAsync(ct);

                // 4. Параллельно получаем funding rate для каждого инструмента
                var validRates = await GetAllFundingRatesAsync(swapInstruments, ct);

                var rates = new List<CurrentFundingRate>(validRates.Count);

                foreach (var rate in validRates)
                {
                    markPrices.TryGetValue(rate.Symbol, out var markPrice);
                    indexPrices.TryGetValue(rate.Symbol, out var indexPrice);

                    rates.Add(CreateFundingRate(
                        rate.Symbol,
                        markPrice,
                        indexPrice,
                        rate.FundingRate ?? 0,
                        rate.FundingTime,
                        CalculateFundingIntervalHours(rate.FundingTime, rate.NextFundingTime)));
                }

                _logger.LogDebug("[OKX] Collected {Count} funding rates", rates.Count);
                return rates;
            },
            cancellationToken);
    }

    private async Task<List<OKXInstrument>> GetAllInstrumentsAsync(CancellationToken ct)
    {
        var instrumentsResult = await _okxClient.UnifiedApi.ExchangeData
            .GetSymbolsAsync(InstrumentType.Swap, ct: ct);

        if (!instrumentsResult.Success)
        {
            _logger.LogError("[OKX] Failed to get instruments: {Error}", instrumentsResult.Error?.Message);
            return [];
        }

        var swapInstruments = instrumentsResult.Data
            .Where(i => IsValidSymbol(i.Symbol))
            .ToList();

        _logger.LogDebug("[OKX] Found {Count} swap instruments", swapInstruments.Count);
        return swapInstruments;
    }

    private async Task<Dictionary<string, decimal>> GetMarkPricesMapAsync(CancellationToken ct)
    {
        var result = await _okxClient.UnifiedApi.ExchangeData.GetMarkPricesAsync(InstrumentType.Swap, ct: ct);

        if (!result.Success)
        {
            _logger.LogError("[OKX] Failed to get mark prices: {Error}", result.Error?.Message);
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }

        return result.Data
            .Where(p => IsValidSymbol(p.Symbol))
            .ToDictionary(
                p => p.Symbol,
                p => p.MarkPrice ?? 0,
                StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<string, decimal>> GetIndexPricesMapAsync(CancellationToken ct)
    {
        var result = await _okxClient.UnifiedApi.ExchangeData.GetIndexTickersAsync("USDT", ct: ct);

        if (!result.Success)
        {
            _logger.LogError("[OKX] Failed to get index prices: {Error}", result.Error?.Message);
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }

        return result.Data
            .ToDictionary(
                p => ConvertToExchangeSymbol(p.Symbol), // В этом запросе OKX возвращает в формате "BTC-USDT"
                p => p.IndexPrice ?? 0,
                StringComparer.OrdinalIgnoreCase);
    }

    private async Task<List<OKXFundingRate>> GetAllFundingRatesAsync(List<OKXInstrument> swapInstruments,
        CancellationToken ct)
    {
        var tasks = swapInstruments.Select(async instrument =>
        {
            var rateResult = await _okxClient.UnifiedApi.ExchangeData
                .GetFundingRatesAsync(instrument.Symbol, ct);

            if (!rateResult.Success || rateResult.Data.Length == 0)
                return null;

            var rate = rateResult.Data.First();

            // Пропускаем неактивные инструменты
            if (rate.FundingRate == 0 && rate.NextFundingTime == default)
                return null;

            return rate;
        });

        var ratesResults = await Task.WhenAll(tasks);
        var validRates = ratesResults.Where(r => r != null).Select(r => r!).ToList();
        return validRates;
    }

    private static int CalculateFundingIntervalHours(DateTime fundingTime, DateTime nextFundingTime)
    {
        var diff = nextFundingTime - fundingTime;
        var hours = (int)diff.TotalHours;

        return hours is > 0 and <= 24 ? hours : 8;
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
                limit = 100; // OKX позволяет максимум 100 записей

                var result = await _okxClient.UnifiedApi.ExchangeData
                    .GetFundingRateHistoryAsync(symbol, fromTime, toTime, limit, ct);

                if (!result.Success)
                {
                    _logger.LogError("[OKX] Historical API Error for {Symbol}: {Error}",
                        symbol, result.Error?.Message);
                    return [];
                }

                var historicalRates = new List<HistoricalFundingRate>(result.Data.Length);

                foreach (var rate in result.Data)
                    historicalRates.Add(CreateHistoricalFundingRate(
                        rate.Symbol,
                        rate.FundingRate,
                        rate.FundingTime));

                _logger.LogDebug("[OKX] Collected {Count} funding rates history", historicalRates.Count);
                return historicalRates;
            },
            cancellationToken);
    }

    public override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteApiCallAsync(
                "GetServerTime",
                async ct => await _okxClient.UnifiedApi.ExchangeData.GetServerTimeAsync(ct),
                cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
    }
}