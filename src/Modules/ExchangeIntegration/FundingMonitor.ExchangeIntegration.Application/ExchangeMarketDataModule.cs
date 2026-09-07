using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.SharedKernel;

namespace FundingMonitor.ExchangeIntegration.Application;

public sealed class ExchangeMarketDataModule : IExchangeMarketDataModule
{
    private readonly IReadOnlyDictionary<ExchangeId, IExchangeGateway> _gateways;

    public ExchangeMarketDataModule(IEnumerable<IExchangeGateway> gateways)
    {
        _gateways = gateways.ToDictionary(gateway => gateway.Exchange);
    }

    public Task<Result<IReadOnlyList<CurrentRateSnapshot>>> GetCurrentRatesAsync(
        ExchangeId exchange,
        CancellationToken cancellationToken) =>
        _gateways.TryGetValue(exchange, out var gateway)
            ? gateway.GetCurrentRatesAsync(cancellationToken)
            : Task.FromResult(NotSupported<IReadOnlyList<CurrentRateSnapshot>>(exchange));

    public Task<Result<IReadOnlyList<HistoricalRatePoint>>> GetHistoricalRatesAsync(
        HistoricalRatesRequest request,
        CancellationToken cancellationToken) =>
        _gateways.TryGetValue(request.Exchange, out var gateway)
            ? gateway.GetHistoricalRatesAsync(request, cancellationToken)
            : Task.FromResult(NotSupported<IReadOnlyList<HistoricalRatePoint>>(request.Exchange));

    public async Task<IReadOnlyList<ExchangeHealthSnapshot>> CheckHealthAsync(
        CancellationToken cancellationToken)
    {
        var checks = _gateways.Values.Select(gateway => gateway.CheckHealthAsync(cancellationToken));
        var snapshots = await Task.WhenAll(checks);
        return snapshots.OrderBy(snapshot => snapshot.Exchange.Value, StringComparer.Ordinal).ToArray();
    }

    private static Result<T> NotSupported<T>(ExchangeId exchange) =>
        Result<T>.Failure(
            new Error(
                "Exchange.NotSupported",
                $"Exchange '{exchange}' is not supported.",
                ErrorType.Validation));
}
