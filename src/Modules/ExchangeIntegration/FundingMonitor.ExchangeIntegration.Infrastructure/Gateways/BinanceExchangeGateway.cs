using Binance.Net.Clients;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.ExchangeIntegration.Domain;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.ExchangeIntegration.Infrastructure.Gateways;

public sealed class BinanceExchangeGateway : BaseExchangeGateway
{
    private readonly BinanceRestClient _client;
    private readonly ILogger<BinanceExchangeGateway> _logger;

    public BinanceExchangeGateway(
        BinanceRestClient client,
        SymbolNormalizer symbolNormalizer,
        TimeProvider timeProvider,
        ILogger<BinanceExchangeGateway> logger)
        : base(symbolNormalizer, timeProvider, logger)
    {
        _client = client;
        _logger = logger;
    }

    public override ExchangeId Exchange => ExchangeId.Parse("Binance");

    public override Task<Result<IReadOnlyList<CurrentRateSnapshot>>> GetCurrentRatesAsync(
        CancellationToken cancellationToken) =>
        ExecuteAsync<IReadOnlyList<CurrentRateSnapshot>>(
            "GetCurrentRates",
            async token =>
            {
                var markPricesTask = _client.UsdFuturesApi.ExchangeData.GetMarkPricesAsync(token);
                var intervalsTask = GetFundingIntervalsAsync(token);
                await Task.WhenAll(markPricesTask, intervalsTask);

                var markPrices = await markPricesTask;
                EnsureSuccess(markPrices);
                var intervals = await intervalsTask;
                var data = markPrices.Data
                           ?? throw new InvalidOperationException("Binance returned no mark-price data.");
                var rates = new List<CurrentRateSnapshot>(data.Length);

                foreach (var item in data)
                {
                    if (!SymbolNormalizer.IsValid(item.Symbol, Exchange))
                        continue;
                    if (item.NextFundingTime < item.Timestamp)
                        continue;
                    if (item is { EstimatedSettlePrice: 0, FundingRate: 0 })
                        continue;
                    if (!intervals.TryGetValue(item.Symbol, out var intervalHours))
                        continue;

                    rates.Add(CreateCurrentRate(
                        item.Symbol,
                        item.MarkPrice,
                        item.IndexPrice,
                        item.FundingRate ?? 0,
                        item.NextFundingTime,
                        intervalHours));
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
                    Exchange.Value, request.Symbol.Value, request.From, request.To, request.Limit);
                var exchangeSymbol = SymbolNormalizer.ToExchangeSymbol(request.Symbol, Exchange);
                var result = await _client.UsdFuturesApi.ExchangeData.GetFundingRatesAsync(
                    exchangeSymbol,
                    request.From.UtcDateTime,
                    request.To.UtcDateTime,
                    request.Limit,
                    token);
                EnsureSuccess(result);

                var data = result.Data
                           ?? throw new InvalidOperationException("Binance returned no funding history.");
                return data
                    .Select(item => CreateHistoricalRate(
                        item.Symbol,
                        item.FundingRate,
                        item.FundingTime))
                    .ToArray();
            },
            cancellationToken);

    public override async Task<ExchangeHealthSnapshot> CheckHealthAsync(
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(
            "Ping",
            async token =>
            {
                var response = await _client.UsdFuturesApi.ExchangeData.PingAsync(token);
                EnsureSuccess(response);
                return true;
            },
            cancellationToken);

        return new ExchangeHealthSnapshot(
            Exchange,
            result.IsSuccess ? ExchangeHealthStatus.Available : ExchangeHealthStatus.Unavailable,
            result.Error?.Code);
    }

    private async Task<Dictionary<string, int>> GetFundingIntervalsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _client.UsdFuturesApi.ExchangeData.GetFundingInfoAsync(cancellationToken);
            if (!result.Success)
            {
                _logger.LogWarning("Funding intervals unavailable for {Exchange}; current snapshot may be incomplete: {ErrorCode}", Exchange.Value, result.Error?.ErrorCode);
                return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            }

            var data = result.Data;
            if (data is null)
            {
                _logger.LogWarning("Funding intervals missing for {Exchange}; current snapshot may be incomplete", Exchange.Value);
                return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            }

            return data
                .Where(info => SymbolNormalizer.IsValid(info.Symbol, Exchange) && info.FundingIntervalHours > 0)
                .ToDictionary(
                    info => info.Symbol,
                    info => info.FundingIntervalHours,
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Funding intervals unavailable for {Exchange}; current snapshot may be incomplete", Exchange.Value);
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
