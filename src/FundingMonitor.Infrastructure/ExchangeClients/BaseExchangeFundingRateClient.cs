using System.Diagnostics;
using System.Net;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Exceptions;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.ExchangeClients;

/// <summary>
///     Базовый класс для клиентов бирж с единой обработкой ошибок
/// </summary>
public abstract class BaseExchangeFundingRateClient : IExchangeFundingRateClient
{
    private readonly ILogger _logger;
    private readonly ISymbolParser _symbolParser;

    protected BaseExchangeFundingRateClient(
        ILogger logger,
        ISymbolParser symbolParser)
    {
        _logger = logger;
        _symbolParser = symbolParser;
    }

    public abstract ExchangeType ExchangeType { get; }

    public abstract Task<List<CurrentFundingRate>> GetCurrentFundingRatesAsync(CancellationToken cancellationToken);

    public abstract Task<List<HistoricalFundingRate>> GetHistoricalFundingRatesAsync(
        string symbol, DateTime fromTime, DateTime toTime, int limit, CancellationToken cancellationToken);

    public abstract Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Выполняет запрос к API с глобальной обработкой ошибок
    /// </summary>
    protected async Task<T> ExecuteApiCallAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("[{Exchange}] {Operation} started", ExchangeType, operationName);

            var result = await action(cancellationToken);

            sw.Stop();
            _logger.LogDebug("[{Exchange}] {Operation} completed in {Elapsed}ms",
                ExchangeType, operationName, sw.ElapsedMilliseconds);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogDebug("[{Exchange}] {Operation} cancelled", ExchangeType, operationName);
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[{Exchange}] {Operation} connection timeout after {Elapsed}ms",
                ExchangeType, operationName, sw.ElapsedMilliseconds);
            throw new TimeoutException($"Connection timeout for {ExchangeType}", ex);
        }
        catch (OperationCanceledException ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[{Exchange}] {Operation} timed out after {Elapsed}ms",
                ExchangeType, operationName, sw.ElapsedMilliseconds);
            throw new TimeoutException($"Operation '{operationName}' for {ExchangeType} timed out", ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue &&
                                              ex.StatusCode.Value >= HttpStatusCode.InternalServerError &&
                                              ex.StatusCode.Value < HttpStatusCode.Ambiguous)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[{Exchange}] {Operation} server error ({StatusCode}) after {Elapsed}ms",
                ExchangeType, operationName, ex.StatusCode, sw.ElapsedMilliseconds);
            throw new ExchangeApiException(ExchangeType, $"Server error: {ex.Message}", (int)ex.StatusCode.Value);
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue &&
                                              ex.StatusCode.Value >= HttpStatusCode.BadRequest &&
                                              ex.StatusCode.Value < HttpStatusCode.InternalServerError)
        {
            sw.Stop();
            _logger.LogError(ex, "[{Exchange}] {Operation} client error ({StatusCode}) after {Elapsed}ms",
                ExchangeType, operationName, ex.StatusCode, sw.ElapsedMilliseconds);
            throw new ExchangeApiException(ExchangeType, $"Client error: {ex.Message}", (int)ex.StatusCode.Value);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[{Exchange}] {Operation} unexpected error after {Elapsed}ms",
                ExchangeType, operationName, sw.ElapsedMilliseconds);
            throw new ExchangeApiException(ExchangeType, $"Unexpected error: {ex.Message}");
        }
    }

    protected CurrentFundingRate CreateFundingRate(
        string symbol,
        decimal markPrice,
        decimal indexPrice,
        decimal fundingRate,
        DateTime? nextFundingTime,
        int? fundingIntervalHours = null)
    {
        var parsed = _symbolParser.Parse(symbol, ExchangeType);

        return new CurrentFundingRate
        {
            Exchange = ExchangeType,
            NormalizedSymbol = $"{parsed.Base}-{parsed.Quote}",
            MarkPrice = markPrice,
            IndexPrice = indexPrice,
            FundingRate = fundingRate,
            FundingIntervalHours = fundingIntervalHours,
            NextFundingTime = nextFundingTime,
            LastCheck = DateTime.UtcNow,
            IsActive = true,
            BaseAsset = parsed.Base,
            QuoteAsset = "USDT"
        };
    }

    protected HistoricalFundingRate CreateHistoricalFundingRate(
        string symbol,
        decimal fundingRate,
        DateTime fundingTime)
    {
        var parsed = _symbolParser.Parse(symbol, ExchangeType);

        return new HistoricalFundingRate
        {
            Exchange = ExchangeType,
            NormalizedSymbol = $"{parsed.Base}-{parsed.Quote}",
            FundingRate = fundingRate,
            FundingTime = fundingTime,
            CollectedAt = DateTime.UtcNow
        };
    }

    protected virtual bool IsValidSymbol(string symbol)
    {
        return symbol.EndsWith("USDT");
    }
}