using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.ExchangeIntegration.Domain;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Logging;
using OKX.Net.Clients;
using OKX.Net.Enums;
using OKX.Net.Objects.Public;

namespace FundingMonitor.ExchangeIntegration.Infrastructure.Gateways;

public sealed class OkxExchangeGateway : BaseExchangeGateway
{
    private readonly OKXRestClient _client;

    public OkxExchangeGateway(
        OKXRestClient client,
        SymbolNormalizer symbolNormalizer,
        TimeProvider timeProvider,
        ILogger<OkxExchangeGateway> logger)
        : base(symbolNormalizer, timeProvider, logger)
    {
        _client = client;
    }

    public override ExchangeId Exchange => ExchangeId.Parse("OKX");

    public override Task<Result<IReadOnlyList<CurrentRateSnapshot>>> GetCurrentRatesAsync(
        CancellationToken cancellationToken) =>
        ExecuteAsync<IReadOnlyList<CurrentRateSnapshot>>(
            "GetCurrentRates",
            async token =>
            {
                var instrumentsTask = GetAllInstrumentsAsync(token);
                var markPricesTask = GetMarkPricesMapAsync(token);
                var indexPricesTask = GetIndexPricesMapAsync(token);
                var fundingRatesTask = _client.UnifiedApi.ExchangeData.GetFundingRatesAsync("ANY", token);
                var quotesTask = GetQuotesAsync(async () =>
                {
                    var response = await _client.UnifiedApi.ExchangeData.GetTickersAsync(InstrumentType.Swap, ct: token);
                    EnsureSuccess(response);
                    var data = response.Data ?? throw new InvalidOperationException("Exchange returned no quote data.");
                    return data.ToDictionary(item => item.Symbol,
                        item => (item.BestBidPrice, item.BestAskPrice),
                        StringComparer.OrdinalIgnoreCase);
                }, token);
                await Task.WhenAll(instrumentsTask, markPricesTask, indexPricesTask, fundingRatesTask, quotesTask);

                var symbols = (await instrumentsTask).Select(item => item.Symbol)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var markPrices = await markPricesTask;
                var indexPrices = await indexPricesTask;
                var fundingResult = await fundingRatesTask;
                EnsureSuccess(fundingResult);
                var fundingRates = fundingResult.Data
                    ?? throw new InvalidOperationException("OKX returned no funding rates.");
                var quotes = await quotesTask;
                var rates = new List<CurrentRateSnapshot>(symbols.Count);

                foreach (var rate in fundingRates)
                {
                    if (!symbols.Contains(rate.Symbol))
                        continue;
                    var quote = quotes.GetValueOrDefault(rate.Symbol);
                    markPrices.TryGetValue(rate.Symbol, out var markPrice);
                    indexPrices.TryGetValue(rate.Symbol, out var indexPrice);
                    rates.Add(CreateCurrentRate(
                        rate.Symbol,
                        markPrice,
                        indexPrice,
                        rate.FundingRate,
                        rate.FundingTime,
                        CalculateFundingIntervalHours(rate.FundingTime, rate.NextFundingTime),
                        quote.Bid,
                        quote.Ask));
                }

                return rates;
            },
            cancellationToken);

    public override Task<Result<IReadOnlyList<HistoricalRatePoint>>> GetHistoricalRatesAsync(
        HistoricalRatesRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync<IReadOnlyList<HistoricalRatePoint>>(
            "GetHistoricalRates",
            async token =>
            {
                Logger.LogDebug("Requesting funding history page for {Exchange} {Symbol}: {From} to {To}, limit {Limit}",
                    Exchange.Value, request.Symbol.Value, request.From, request.To, Math.Min(request.Limit, 100));
                var exchangeSymbol = SymbolNormalizer.ToExchangeSymbol(request.Symbol, Exchange);
                var result = await _client.UnifiedApi.ExchangeData.GetFundingRateHistoryAsync(
                    exchangeSymbol,
                    request.From.UtcDateTime,
                    request.To.UtcDateTime,
                    Math.Min(request.Limit, 100),
                    token);
                EnsureSuccess(result);

                var data = result.Data
                           ?? throw new InvalidOperationException("OKX returned no funding history.");
                return data
                    .Select(rate => CreateHistoricalRate(
                        rate.Symbol,
                        rate.FundingRate,
                        rate.FundingTime))
                    .ToArray();
            },
            cancellationToken);

    public override async Task<ExchangeHealthSnapshot> CheckHealthAsync(
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(
            "GetServerTime",
            async token =>
            {
                var response = await _client.UnifiedApi.ExchangeData.GetServerTimeAsync(token);
                EnsureSuccess(response);
                return true;
            },
            cancellationToken);

        return new ExchangeHealthSnapshot(
            Exchange,
            result.IsSuccess ? ExchangeHealthStatus.Available : ExchangeHealthStatus.Unavailable);
    }

    private async Task<List<OKXInstrument>> GetAllInstrumentsAsync(CancellationToken cancellationToken)
    {
        var result = await _client.UnifiedApi.ExchangeData.GetSymbolsAsync(
            InstrumentType.Swap,
            ct: cancellationToken);
        EnsureSuccess(result);
        var data = result.Data
                   ?? throw new InvalidOperationException("OKX returned no instruments.");
        return data.Where(item => SymbolNormalizer.IsValid(item.Symbol, Exchange)).ToList();
    }

    private async Task<Dictionary<string, decimal>> GetMarkPricesMapAsync(
        CancellationToken cancellationToken)
    {
        var result = await _client.UnifiedApi.ExchangeData.GetMarkPricesAsync(
            InstrumentType.Swap,
            ct: cancellationToken);
        EnsureSuccess(result);
        var data = result.Data
                   ?? throw new InvalidOperationException("OKX returned no mark prices.");
        return data
            .Where(item => SymbolNormalizer.IsValid(item.Symbol, Exchange))
            .ToDictionary(
                item => item.Symbol,
                item => item.MarkPrice ?? 0,
                StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<string, decimal>> GetIndexPricesMapAsync(
        CancellationToken cancellationToken)
    {
        var result = await _client.UnifiedApi.ExchangeData.GetIndexTickersAsync(
            "USDT",
            ct: cancellationToken);
        EnsureSuccess(result);
        var data = result.Data
                   ?? throw new InvalidOperationException("OKX returned no index prices.");
        return data.ToDictionary(
            item => item.Symbol.EndsWith("-SWAP", StringComparison.OrdinalIgnoreCase)
                ? item.Symbol
                : $"{item.Symbol}-SWAP",
            item => item.IndexPrice ?? 0,
            StringComparer.OrdinalIgnoreCase);
    }

    private static int? CalculateFundingIntervalHours(DateTime fundingTime, DateTime nextFundingTime)
    {
        if (fundingTime == default || nextFundingTime == default)
            return null;
        var hours = (nextFundingTime - fundingTime).TotalHours;
        return hours is > 0 and <= 24 && hours == Math.Truncate(hours) ? (int)hours : null;
    }
}
