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
                var instruments = await GetAllInstrumentsAsync(token);
                var markPrices = await GetMarkPricesMapAsync(token);
                var indexPrices = await GetIndexPricesMapAsync(token);
                var fundingRates = await GetAllFundingRatesAsync(instruments, token);
                var rates = new List<CurrentRateSnapshot>(fundingRates.Count);

                foreach (var rate in fundingRates)
                {
                    markPrices.TryGetValue(rate.Symbol, out var markPrice);
                    indexPrices.TryGetValue(rate.Symbol, out var indexPrice);
                    rates.Add(CreateCurrentRate(
                        rate.Symbol,
                        markPrice,
                        indexPrice,
                        rate.FundingRate ?? 0,
                        rate.FundingTime,
                        CalculateFundingIntervalHours(rate.FundingTime, rate.NextFundingTime)));
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
                var exchangeSymbol = SymbolNormalizer.ToExchangeSymbol(request.Symbol, Exchange);
                var result = await _client.UnifiedApi.ExchangeData.GetFundingRateHistoryAsync(
                    exchangeSymbol,
                    request.From.UtcDateTime,
                    request.To.UtcDateTime,
                    Math.Min(request.Limit, 100),
                    token);
                EnsureSuccess(
                    result.Success,
                    result.Error?.Message,
                    result.Error?.ErrorCode,
                    result.Error?.ErrorType,
                    result.Error?.Exception);

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
                EnsureSuccess(
                    response.Success,
                    response.Error?.Message,
                    response.Error?.ErrorCode,
                    response.Error?.ErrorType,
                    response.Error?.Exception);
                return true;
            },
            cancellationToken);

        return new ExchangeHealthSnapshot(
            Exchange,
            result.IsSuccess ? ExchangeHealthStatus.Available : ExchangeHealthStatus.Unavailable,
            result.Error?.Code);
    }

    private async Task<List<OKXInstrument>> GetAllInstrumentsAsync(CancellationToken cancellationToken)
    {
        var result = await _client.UnifiedApi.ExchangeData.GetSymbolsAsync(
            InstrumentType.Swap,
            ct: cancellationToken);
        EnsureSuccess(
            result.Success,
            result.Error?.Message,
            result.Error?.ErrorCode,
            result.Error?.ErrorType,
            result.Error?.Exception);
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
        EnsureSuccess(
            result.Success,
            result.Error?.Message,
            result.Error?.ErrorCode,
            result.Error?.ErrorType,
            result.Error?.Exception);
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
        EnsureSuccess(
            result.Success,
            result.Error?.Message,
            result.Error?.ErrorCode,
            result.Error?.ErrorType,
            result.Error?.Exception);
        var data = result.Data
                   ?? throw new InvalidOperationException("OKX returned no index prices.");
        return data.ToDictionary(
            item => item.Symbol.EndsWith("-SWAP", StringComparison.OrdinalIgnoreCase)
                ? item.Symbol
                : $"{item.Symbol}-SWAP",
            item => item.IndexPrice ?? 0,
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<List<OKXFundingRate>> GetAllFundingRatesAsync(
        IReadOnlyList<OKXInstrument> instruments,
        CancellationToken cancellationToken)
    {
        var tasks = instruments.Select(async instrument =>
        {
            var result = await _client.UnifiedApi.ExchangeData.GetFundingRatesAsync(
                instrument.Symbol,
                cancellationToken);
            if (!result.Success || result.Data is not { Length: > 0 } data)
                return null;

            var rate = data[0];
            return rate.FundingRate == 0 && rate.NextFundingTime == default ? null : rate;
        });

        var results = await Task.WhenAll(tasks);
        return results.Where(rate => rate is not null).Select(rate => rate!).ToList();
    }

    private static int CalculateFundingIntervalHours(DateTime fundingTime, DateTime nextFundingTime)
    {
        var hours = (int)(nextFundingTime - fundingTime).TotalHours;
        return hours is > 0 and <= 24 ? hours : 8;
    }
}
