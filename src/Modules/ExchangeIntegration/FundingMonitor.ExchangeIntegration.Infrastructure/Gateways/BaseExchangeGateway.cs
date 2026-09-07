using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using FundingMonitor.ExchangeIntegration.Application;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.ExchangeIntegration.Domain;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.ExchangeIntegration.Infrastructure.Gateways;

public abstract class BaseExchangeGateway : IExchangeGateway
{
    private static readonly Meter Meter = new("FundingMonitor.ExchangeIntegration");
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "fundingmonitor.exchange.request.duration",
        "ms");
    private static readonly Counter<long> RequestErrors = Meter.CreateCounter<long>(
        "fundingmonitor.exchange.request.errors");

    private readonly ILogger _logger;

    protected BaseExchangeGateway(
        SymbolNormalizer symbolNormalizer,
        TimeProvider timeProvider,
        ILogger logger)
    {
        SymbolNormalizer = symbolNormalizer;
        TimeProvider = timeProvider;
        _logger = logger;
    }

    public abstract ExchangeId Exchange { get; }

    protected SymbolNormalizer SymbolNormalizer { get; }

    protected TimeProvider TimeProvider { get; }

    public abstract Task<Result<IReadOnlyList<CurrentRateSnapshot>>> GetCurrentRatesAsync(
        CancellationToken cancellationToken);

    public abstract Task<Result<IReadOnlyList<HistoricalRatePoint>>> GetHistoricalRatesAsync(
        HistoricalRatesRequest request,
        CancellationToken cancellationToken);

    public abstract Task<ExchangeHealthSnapshot> CheckHealthAsync(CancellationToken cancellationToken);

    protected async Task<Result<T>> ExecuteAsync<T>(
        string operation,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var value = await action(cancellationToken);
            return Result<T>.Success(value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var error = Classify(exception);
            var tags = new TagList
            {
                { "exchange", Exchange.Value },
                { "error.code", error.Code }
            };
            RequestErrors.Add(1, tags);
            _logger.LogWarning(
                exception,
                "[{Exchange}] {Operation} failed with {ErrorCode}",
                Exchange,
                operation,
                error.Code);
            return Result<T>.Failure(error);
        }
        finally
        {
            RequestDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("exchange", Exchange.Value));
        }
    }

    protected static void EnsureSuccess(
        bool success,
        string? message,
        object? code = null,
        object? errorType = null,
        Exception? innerException = null)
    {
        if (!success)
            throw new ExchangeGatewayFailureException(
                $"{errorType}: {message ?? "Unknown upstream error"}",
                code?.ToString(),
                innerException);
    }

    protected CurrentRateSnapshot CreateCurrentRate(
        string exchangeSymbol,
        decimal markPrice,
        decimal indexPrice,
        decimal fundingRate,
        DateTime? nextFundingTime,
        int? fundingIntervalHours) =>
        new(
            Exchange,
            SymbolNormalizer.Normalize(exchangeSymbol, Exchange),
            markPrice,
            indexPrice,
            fundingRate,
            fundingIntervalHours,
            nextFundingTime is null ? null : AsUtc(nextFundingTime.Value),
            null,
            TimeProvider.GetUtcNow());

    protected HistoricalRatePoint CreateHistoricalRate(
        string exchangeSymbol,
        decimal fundingRate,
        DateTime fundingTime) =>
        new(
            Exchange,
            SymbolNormalizer.Normalize(exchangeSymbol, Exchange),
            fundingRate,
            AsUtc(fundingTime),
            TimeProvider.GetUtcNow());

    private static DateTimeOffset AsUtc(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
        return new DateTimeOffset(utc);
    }

    private static Error Classify(Exception exception)
    {
        var chain = EnumerateExceptionChain(exception).ToArray();
        var statusCode = chain.OfType<HttpRequestException>()
            .Select(item => item.StatusCode)
            .FirstOrDefault(item => item.HasValue);
        var text = string.Join(
            " ",
            chain.Select(item =>
                $"{(item as ExchangeGatewayFailureException)?.UpstreamCode} {item.Message}"));

        if (exception is TimeoutException or TaskCanceledException)
            return CreateError("Exchange.Timeout", ErrorType.Timeout);
        if (statusCode == HttpStatusCode.TooManyRequests ||
            Contains(text, "429", "rate limit", "ratelimit", "too many"))
            return CreateError("Exchange.RateLimited", ErrorType.Unavailable);
        if (statusCode == HttpStatusCode.Forbidden || Contains(text, "403", "restricted location", "geo"))
            return CreateError("Exchange.GeoRestricted", ErrorType.Unavailable);
        if (statusCode == HttpStatusCode.Unauthorized || Contains(text, "401", "authentication", "unauthorized"))
            return CreateError("Exchange.Authentication", ErrorType.Unavailable);
        if (statusCode == HttpStatusCode.BadRequest || Contains(text, "400", "invalid request", "invalid symbol"))
            return CreateError("Exchange.InvalidRequest", ErrorType.Validation);

        return CreateError("Exchange.UpstreamUnavailable", ErrorType.Unavailable);
    }

    private static bool Contains(string value, params string[] candidates) =>
        candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<Exception> EnumerateExceptionChain(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
            yield return current;
    }

    private static Error CreateError(string code, ErrorType type) =>
        new(code, "The exchange request could not be completed.", type);

    protected sealed class ExchangeGatewayFailureException(
        string message,
        string? upstreamCode,
        Exception? innerException)
        : Exception(message, innerException)
    {
        internal string? UpstreamCode { get; } = upstreamCode;
    }
}
