using System.Threading.RateLimiting;
using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Exceptions;
using FundingMonitor.Core.Interfaces.Clients;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.RateLimiting;

namespace FundingMonitor.Infrastructure.ExchangeClients;

/// <summary>
///     Декоратор, добавляющий rate limiting к любому IExchangeFundingRateClient
/// </summary>
public class RateLimitedFundingRateClient : IExchangeFundingRateClient
{
    private readonly IExchangeFundingRateClient _innerClient;
    private readonly ILogger<RateLimitedFundingRateClient> _logger;
    private readonly ResiliencePipeline _rateLimiterPipeline;

    public RateLimitedFundingRateClient(
        IExchangeFundingRateClient innerClient,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<RateLimitedFundingRateClient> logger)
    {
        _innerClient = innerClient;
        ExchangeType = innerClient.ExchangeType;
        _logger = logger;

        var options = rateLimitOptions.Value;

        var limits = ExchangeType switch
        {
            ExchangeType.Binance => options.Binance,
            ExchangeType.Bybit => options.Bybit,
            _ => throw new NotSupportedException($"Unsupported exchange: {ExchangeType}")
        };

        var rateLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = limits.PermitLimit,
            SegmentsPerWindow = limits.SegmentsPerWindow,
            Window = TimeSpan.FromSeconds(limits.WindowSeconds),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = limits.QueueLimit,
            AutoReplenishment = true
        });

        _rateLimiterPipeline = new ResiliencePipelineBuilder()
            .AddRateLimiter(rateLimiter)
            .Build();
    }

    public ExchangeType ExchangeType { get; }

    public async Task<List<CurrentFundingRate>> GetCurrentFundingRatesAsync(CancellationToken cancellationToken)
    {
        return await ExecuteWithRateLimitAsync(
            () => _innerClient.GetCurrentFundingRatesAsync(cancellationToken),
            nameof(GetCurrentFundingRatesAsync),
            cancellationToken);
    }

    public async Task<List<HistoricalFundingRate>> GetHistoricalFundingRatesAsync(
        string symbol, DateTime fromTime, DateTime toTime, int limit, CancellationToken cancellationToken)
    {
        return await ExecuteWithRateLimitAsync(
            () => _innerClient.GetHistoricalFundingRatesAsync(symbol, fromTime, toTime, limit, cancellationToken),
            $"{nameof(GetHistoricalFundingRatesAsync)}:{symbol}",
            cancellationToken);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        return await ExecuteWithRateLimitAsync(
            () => _innerClient.IsAvailableAsync(cancellationToken),
            nameof(IsAvailableAsync),
            cancellationToken);
    }

    private async Task<T> ExecuteWithRateLimitAsync<T>(
        Func<Task<T>> action,
        string operationName,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        const int maxRetries = 3;

        while (attempt < maxRetries)
            try
            {
                // Polly сама ждёт, если лимит исчерпан
                return await _rateLimiterPipeline.ExecuteAsync(
                    async _ => await action(),
                    cancellationToken);
            }
            catch (RateLimiterRejectedException ex)
            {
                attempt++;

                if (attempt >= maxRetries)
                {
                    throw new ExchangeRateLimitException(ExchangeType, "Rate limit exceeded", ex);
                }

                // Экспоненциальная задержка между попытками
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt) * 5);
                _logger.LogWarning(
                    "[{Exchange}] Rate limit hit for {Operation}, retry {Attempt}/{MaxRetries} after {Delay}s",
                    ExchangeType, operationName, attempt, maxRetries, delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }

        throw new InvalidOperationException($"Should not reach here for {ExchangeType}:{operationName}");
    }
}