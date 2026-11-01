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

    protected readonly ILogger Logger;

    protected BaseExchangeGateway(
        SymbolNormalizer symbolNormalizer,
        TimeProvider timeProvider,
        ILogger logger)
    {
        SymbolNormalizer = symbolNormalizer;
        TimeProvider = timeProvider;
        Logger = logger;
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
            Logger.LogDebug("Exchange {Exchange} operation {Operation} completed in {DurationMs} ms",
                Exchange.Value, operation, stopwatch.Elapsed.TotalMilliseconds);
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
            var upstream = EnumerateExceptionChain(exception).OfType<ExchangeGatewayFailureException>().FirstOrDefault();
            var statusCode = upstream?.StatusCode ?? EnumerateExceptionChain(exception)
                .OfType<HttpRequestException>().Select(item => item.StatusCode).FirstOrDefault(item => item.HasValue);
            Logger.LogWarning(
                "Exchange {Exchange} {Operation} failed: {ErrorCode}; HTTP {HttpStatusCode}; {Reason}",
                Exchange.Value, operation, error.Code, statusCode is { } status ? (int)status : null,
                DiagnosticMessage(error.Code, upstream?.Message ?? exception.Message));
            return Result<T>.Failure(error);
        }
        finally
        {
            RequestDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("exchange", Exchange.Value));
        }
    }

    protected static void EnsureSuccess<T>(CryptoExchange.Net.Objects.HttpResult<T> result)
    {
        if (result.Success) return;
        var message = result.Error?.Message ?? "Unknown upstream error";
        // Use the captured response only to identify a known CDN denial; never log the body.
        if (Contains(result.OriginalData ?? "", "block access from your country", "restricted location"))
            message = "CloudFront or exchange blocked access from your country";
        throw new ExchangeGatewayFailureException(message,
            result.Error?.ErrorCode?.ToString() ?? ReadBybitCode(result.OriginalData), result.Error?.Exception)
        {
            StatusCode = result.ResponseStatusCode,
            SourceErrorType = result.Error?.ErrorType.ToString()
        };
    }

    private static string? ReadBybitCode(string? body)
    {
        if (string.IsNullOrEmpty(body)) return null;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object &&
                document.RootElement.TryGetProperty("retCode", out var code) &&
                code.ValueKind is System.Text.Json.JsonValueKind.Number or System.Text.Json.JsonValueKind.String)
                return code.ToString();
        }
        catch (System.Text.Json.JsonException) { }
        return null;
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

    private Error Classify(Exception exception)
    {
        var chain = EnumerateExceptionChain(exception).ToArray();
        var upstream = chain.OfType<ExchangeGatewayFailureException>().FirstOrDefault();
        var statusCode = upstream?.StatusCode ?? chain.OfType<HttpRequestException>()
            .Select(item => item.StatusCode)
            .FirstOrDefault(item => item.HasValue);
        var text = string.Join(
            " ",
            chain.Select(item =>
                $"{(item as ExchangeGatewayFailureException)?.UpstreamCode} {item.Message}"));

        if (chain.Any(item => item is TimeoutException or TaskCanceledException))
            return CreateError("Exchange.Timeout", ErrorType.Timeout);
        if (statusCode == HttpStatusCode.TooManyRequests ||
            (Exchange.Value == "Bybit" && upstream?.UpstreamCode == "10006") ||
            Contains(upstream?.SourceErrorType ?? "", "RateLimit") ||
            Contains(text, "rate limit", "ratelimit", "too many", "access too frequent"))
            return CreateError("Exchange.RateLimited", ErrorType.Unavailable);
        if (Contains(text, "restricted location", "block access from your country", "blocked access from your country", "country is restricted", "geographical restriction"))
            return CreateError("Exchange.GeoRestricted", ErrorType.Unavailable);
        if (statusCode == HttpStatusCode.Forbidden)
            return CreateError("Exchange.AccessDenied", ErrorType.Unavailable);
        if (statusCode == HttpStatusCode.Unauthorized || Contains(text, "authentication", "unauthorized"))
            return CreateError("Exchange.Authentication", ErrorType.Unavailable);
        if (statusCode == HttpStatusCode.BadRequest || Contains(text, "invalid request", "invalid symbol"))
            return CreateError("Exchange.InvalidRequest", ErrorType.Validation);

        if (statusCode is >= HttpStatusCode.InternalServerError)
            return CreateError("Exchange.UpstreamUnavailable", ErrorType.Unavailable);
        if (Contains(upstream?.SourceErrorType ?? text, "deserializ") || chain.Any(item => item is System.Text.Json.JsonException))
            return CreateError("Exchange.InvalidResponse", ErrorType.Unavailable);
        if (chain.Any(item => item is HttpRequestException or System.Net.Sockets.SocketException))
            return CreateError("Exchange.Network", ErrorType.Unavailable);
        return CreateError("Exchange.UpstreamUnavailable", ErrorType.Unavailable);
    }

    private static string ShortMessage(string value) =>
        value.Replace('\r', ' ').Replace('\n', ' ')[..Math.Min(value.Length, 400)];

    private static string DiagnosticMessage(string code, string message)
    {
        if (code == "Exchange.GeoRestricted") return "Access blocked for the request's country or region";
        if (code == "Exchange.InvalidResponse") return "Upstream response could not be deserialized (body omitted)";
        // SDK deserialization messages can contain the complete upstream response.
        var bodyStart = message.IndexOf("invalid JSON", StringComparison.OrdinalIgnoreCase);
        if (bodyStart >= 0) message = message[..bodyStart] + "invalid JSON (body omitted)";
        if (message.Contains("<html", StringComparison.OrdinalIgnoreCase) || message.Contains("<!doctype", StringComparison.OrdinalIgnoreCase))
            return "Upstream returned an HTML error page (body omitted)";
        return ShortMessage(message);
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
        internal HttpStatusCode? StatusCode { get; init; }
        internal string? SourceErrorType { get; init; }
    }
}
