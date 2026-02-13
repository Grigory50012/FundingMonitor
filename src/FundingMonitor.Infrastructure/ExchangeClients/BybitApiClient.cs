using System.Diagnostics;
using Bybit.Net;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using CryptoExchange.Net.Objects;
using FundingMonitor.Application.Interfaces.Clients;
using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ExchangeType = FundingMonitor.Core.Entities.ExchangeType;

namespace FundingMonitor.Infrastructure.ExchangeClients;

public class BybitApiClient : IExchangeApiClient
{
    private const string QuoteAsset = "USDT";
    private readonly BybitRestClient _bybitClient;
    private readonly ILogger<BybitApiClient> _logger;
    private readonly ISymbolNormalizer _symbolNormalizer;

    public BybitApiClient(
        ILogger<BybitApiClient> logger,
        ISymbolNormalizer symbolNormalizer,
        IOptions<ExchangeOptions> bybitOptions)
    {
        _logger = logger;
        _symbolNormalizer = symbolNormalizer;
        var options = bybitOptions.Value;

        _bybitClient = new BybitRestClient(bybitClientOptions =>
        {
            bybitClientOptions.Environment = BybitEnvironment.Live; // Окружение
            bybitClientOptions.RequestTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds); // Таймаут запроса
            bybitClientOptions.AutoTimestamp = true; // Автоматическая синхронизация времени
            bybitClientOptions.TimestampRecalculationInterval = TimeSpan.FromHours(1); // Интервал пересчета времени
            bybitClientOptions.HttpVersion = new Version(2, 0); // Версия HTTP протокола
            bybitClientOptions.HttpKeepAliveInterval = TimeSpan.FromSeconds(60); // Интервал keep-alive
            bybitClientOptions.RateLimiterEnabled = true; // Ограничение частоты запросов
            bybitClientOptions.RateLimitingBehaviour = RateLimitingBehaviour.Wait; // Поведение при достижении лимита
            bybitClientOptions.OutputOriginalData = false; // Вывод исходных данных JSON
            bybitClientOptions.CachingEnabled = false; // Кэширование отключаем - нужны свежие данные
        });
    }

    public ExchangeType ExchangeType => ExchangeType.Bybit;

    public async Task<List<CurrentFundingRate>> GetAllFundingRatesAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _bybitClient.V5Api.ExchangeData.GetLinearInverseTickersAsync(
                Category.Linear,
                ct: cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("[Bybit] Ошибка: {Error}", result.Error?.Message);
                throw new ExchangeApiException(ExchangeType.Bybit, result.Error?.Message ?? "Unknown error");
            }

            var tickers = result.Data.List;
            var fundingRates = new List<CurrentFundingRate>(tickers.Length);

            foreach (var ticker in tickers)
            {
                if (!ticker.Symbol.EndsWith(QuoteAsset))
                    continue;

                var parsed = _symbolNormalizer.Parse(ticker.Symbol, ExchangeType);

                fundingRates.Add(new CurrentFundingRate
                {
                    Exchange = ExchangeType.Bybit,
                    NormalizedSymbol = parsed.Base + "-" + parsed.Quote,
                    MarkPrice = ticker.MarkPrice,
                    IndexPrice = ticker.IndexPrice,
                    FundingRate = ticker.FundingRate ?? 0,
                    FundingIntervalHours = ticker.FundingInterval,
                    NextFundingTime = ticker.NextFundingTime ?? DateTime.UtcNow,
                    LastCheck = DateTime.UtcNow,
                    IsActive = true,
                    BaseAsset = parsed.Base,
                    QuoteAsset = QuoteAsset
                });
            }

            stopwatch.Stop();
            _logger.LogInformation("[Bybit] собрано {Count} за {ElapsedMilliseconds} мс", fundingRates.Count,
                stopwatch.ElapsedMilliseconds);
            return fundingRates;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[Bybit] Сбор не выполнен");
            throw new ExchangeApiException(ExchangeType.Bybit, $"Bybit API error: {ex.Message}");
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _bybitClient.V5Api.ExchangeData.GetServerTimeAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}