using System.Diagnostics;
using CryptoExchange.Net.Objects.Options;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Infrastructure.ExchangeClients;

public abstract class BaseExchangeApiClient : IExchangeApiClient
{
    protected readonly ILogger Logger;
    protected readonly ExchangeOptions Options;
    protected readonly ISymbolNormalizer SymbolNormalizer;

    protected BaseExchangeApiClient(
        ILogger logger,
        ISymbolNormalizer symbolNormalizer,
        IOptions<ExchangeOptions> options)
    {
        Logger = logger;
        SymbolNormalizer = symbolNormalizer;
        Options = options.Value;
    }

    private static string QuoteAsset => "USDT";

    public abstract ExchangeType ExchangeType { get; }

    public abstract Task<List<CurrentFundingRate>> GetCurrentFundingRatesAsync(CancellationToken cancellationToken);

    public abstract Task<List<HistoricalFundingRate>> GetHistoricalFundingRatesAsync(
        string symbol, DateTime fromTime, DateTime toTime, int limit, CancellationToken cancellationToken);

    public abstract Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

    protected CurrentFundingRate CreateFundingRate(
        string symbol,
        decimal markPrice,
        decimal indexPrice,
        decimal fundingRate,
        DateTime? nextFundingTime,
        int? fundingIntervalHours = null)
    {
        var parsed = SymbolNormalizer.Parse(symbol, ExchangeType);

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
            QuoteAsset = QuoteAsset
        };
    }

    protected HistoricalFundingRate CreateHistoricalFundingRate(
        string symbol,
        decimal fundingRate,
        DateTime fundingTime)
    {
        var parsed = SymbolNormalizer.Parse(symbol, ExchangeType);

        return new HistoricalFundingRate
        {
            Exchange = ExchangeType,
            NormalizedSymbol = $"{parsed.Base}-{parsed.Quote}",
            FundingRate = fundingRate,
            FundingTime = fundingTime,
            CollectedAt = DateTime.UtcNow
        };
    }

    protected async Task<T> ExecuteWithMonitoringAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.LogDebug("[{Exchange}] Начало {Operation}", ExchangeType, operationName);
            var result = await action(cancellationToken);

            stopwatch.Stop();
            Logger.LogInformation("[{Exchange}] {Operation} завершено за {Elapsed}мс",
                ExchangeType, operationName, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            Logger.LogWarning("[{Exchange}] {Operation} отменена после {Elapsed}мс",
                ExchangeType, operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "[{Exchange}] {Operation} не удалось после {Elapsed}мс",
                ExchangeType, operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    protected static bool IsValidSymbol(string symbol)
    {
        return symbol.EndsWith(QuoteAsset);
    }
}