using System.Diagnostics;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.ExchangeClients;

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
    /// Выполняет запрос к API
    /// </summary>
    protected async Task<T> ExecuteApiCallAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await action(cancellationToken);

            sw.Stop();
            _logger.LogDebug("{Operation} completed in {Elapsed}ms", operationName, sw.ElapsedMilliseconds);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogDebug("{Operation} cancelled", operationName);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            sw.Stop();
            _logger.LogWarning("{Operation} timed out after {Elapsed}ms", operationName, sw.ElapsedMilliseconds);
            throw new TimeoutException($"Operation {operationName} timed out", ex);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "{Operation} failed after {Elapsed}ms", operationName, sw.ElapsedMilliseconds);
            throw;
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

    protected static bool IsValidSymbol(string symbol)
    {
        return symbol.EndsWith("USDT");
    }
}