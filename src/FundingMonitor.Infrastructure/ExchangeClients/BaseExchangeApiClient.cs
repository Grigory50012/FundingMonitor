using System.Diagnostics;
using FundingMonitor.Application.Interfaces.Clients;
using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Infrastructure.ExchangeClients;

public abstract class BaseExchangeApiClient : IExchangeApiClient
{
    private readonly ISymbolNormalizer _symbolNormalizer;
    protected readonly ILogger Logger;
    protected readonly ExchangeOptions Options;

    protected BaseExchangeApiClient(
        ILogger logger,
        ISymbolNormalizer symbolNormalizer,
        IOptions<ExchangeOptions> options)
    {
        Logger = logger;
        _symbolNormalizer = symbolNormalizer;
        Options = options.Value;
    }

    private static string QuoteAsset => "USDT";

    public abstract ExchangeType ExchangeType { get; }

    /// <summary>
    ///     Основной метод для получения ставок финансирования
    /// </summary>
    public abstract Task<List<CurrentFundingRate>> GetCurrentFundingRatesAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Проверка доступности биржи
    /// </summary>
    public abstract Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Создает объект CurrentFundingRate из полученных данных
    /// </summary>
    protected CurrentFundingRate CreateFundingRate(
        string symbol,
        decimal markPrice,
        decimal indexPrice,
        decimal fundingRate,
        DateTime? nextFundingTime,
        int? fundingIntervalHours = null)
    {
        try
        {
            var parsed = _symbolNormalizer.Parse(symbol, ExchangeType);

            return new CurrentFundingRate
            {
                Exchange = ExchangeType,
                NormalizedSymbol = $"{parsed.Base}-{parsed.Quote}",
                MarkPrice = markPrice,
                IndexPrice = indexPrice,
                FundingRate = fundingRate,
                FundingIntervalHours = fundingIntervalHours,
                NextFundingTime = nextFundingTime ?? null,
                LastCheck = DateTime.UtcNow,
                IsActive = true,
                BaseAsset = parsed.Base,
                QuoteAsset = QuoteAsset
            };
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[{Exchange}] Failed to parse symbol {Symbol}", ExchangeType, symbol);
            throw;
        }
    }

    /// <summary>
    ///     Выполняет действие с измерением времени и обработкой ошибок
    /// </summary>
    protected async Task<T> ExecuteWithMonitoringAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.LogDebug("[{Exchange}] Starting {Operation}", ExchangeType, operationName);

            var result = await action(cancellationToken);

            stopwatch.Stop();
            Logger.LogInformation("[{Exchange}] {Operation} completed in {ElapsedMilliseconds}ms",
                ExchangeType, operationName, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            Logger.LogWarning("[{Exchange}] {Operation} was cancelled after {ElapsedMilliseconds}ms",
                ExchangeType, operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "[{Exchange}] {Operation} failed after {ElapsedMilliseconds}ms",
                ExchangeType, operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    ///     Фильтрует символы, оставляя только с нужной quote валютой
    /// </summary>
    protected static bool IsValidSymbol(string symbol)
    {
        return symbol.EndsWith(QuoteAsset);
    }
}