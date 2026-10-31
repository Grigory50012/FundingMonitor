using Bybit.Net.Clients;
using Bybit.Net.Enums;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.ExchangeIntegration.Domain;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.ExchangeIntegration.Infrastructure.Gateways;

public sealed class BybitExchangeGateway : BaseExchangeGateway
{
    private readonly BybitRestClient _client;

    public BybitExchangeGateway(
        BybitRestClient client,
        SymbolNormalizer symbolNormalizer,
        TimeProvider timeProvider,
        ILogger<BybitExchangeGateway> logger)
        : base(symbolNormalizer, timeProvider, logger)
    {
        _client = client;
    }

    public override ExchangeId Exchange => ExchangeId.Parse("Bybit");

    public override Task<Result<IReadOnlyList<CurrentRateSnapshot>>> GetCurrentRatesAsync(
        CancellationToken cancellationToken) =>
        ExecuteAsync<IReadOnlyList<CurrentRateSnapshot>>(
            "GetCurrentRates",
            async token =>
            {
                var result = await _client.V5Api.ExchangeData.GetLinearInverseTickersAsync(
                    Category.Linear,
                    ct: token);
                EnsureSuccess(
                    result.Success,
                    result.Error?.Message,
                    result.Error?.ErrorCode,
                    result.Error?.ErrorType,
                    result.Error?.Exception);

                var data = result.Data
                           ?? throw new InvalidOperationException("Bybit returned no ticker data.");
                return data.List
                    .Where(item => SymbolNormalizer.IsValid(item.Symbol, Exchange))
                    .Select(item => CreateCurrentRate(
                        item.Symbol,
                        item.MarkPrice,
                        item.IndexPrice,
                        item.FundingRate ?? 0,
                        item.NextFundingTime,
                        item.FundingInterval))
                    .ToArray();
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
                var result = await _client.V5Api.ExchangeData.GetFundingRateHistoryAsync(
                    Category.Linear,
                    exchangeSymbol,
                    request.From.UtcDateTime,
                    request.To.UtcDateTime,
                    request.Limit,
                    token);
                EnsureSuccess(
                    result.Success,
                    result.Error?.Message,
                    result.Error?.ErrorCode,
                    result.Error?.ErrorType,
                    result.Error?.Exception);

                var data = result.Data
                           ?? throw new InvalidOperationException("Bybit returned no funding history.");
                return data.List
                    .Select(item => CreateHistoricalRate(
                        item.Symbol,
                        item.FundingRate,
                        item.Timestamp))
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
                var response = await _client.V5Api.ExchangeData.GetServerTimeAsync(token);
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
}
