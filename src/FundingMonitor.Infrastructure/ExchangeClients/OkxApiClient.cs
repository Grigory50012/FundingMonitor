using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Core.Entities;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.ExchangeClients;

public class OkxApiClient : BaseExchangeApiClient
{
    private readonly ILogger _logger;
    private readonly ISymbolNormalizer _symbolNormalizer;

    public OkxApiClient(
        HttpClient httpClient,
        ILogger<OkxApiClient> logger,
        ISymbolNormalizer symbolNormalizer,
        ILogger logger1)
        : base(httpClient, logger)
    {
        _symbolNormalizer = symbolNormalizer;
        _logger = logger1;
    }

    public override ExchangeType ExchangeType => ExchangeType.OKX;

    public override async Task<List<CurrentFundingRate>> GetAllFundingRatesAsync(CancellationToken cancellationToken)
    {
        var results = new List<CurrentFundingRate>();

        // 1. Получаем все SWAP инструменты
        var instruments = await GetAllInstrumentsAsync();
        var usdtInstruments = instruments
            .Where(i => i.InstId.EndsWith("USDT-SWAP"))
            .ToList();

        // 2. Получаем все тикеры одним запросом
        var tickersResponse = await GetAsync<OkxTickersResponse>("/api/v5/market/tickers?instType=SWAP");

        if (tickersResponse?.Data == null)
        {
            return results;
        }

        // Создаем словарь тикеров для быстрого поиска
        var tickerDict = tickersResponse.Data
            .Where(t => !string.IsNullOrEmpty(t.InstId))
            .ToDictionary(t => t.InstId, t => t);

        // 4. Для каждого инструмента получаем ставку финансирования
        foreach (var instrument in usdtInstruments)
        {
            // 5. Получаем ставку финансирования для конкретного символа
            var fundingResponse = await GetAsync<OkxFundingRateResponse>(
                $"/api/v5/public/funding-rate?instId={instrument.InstId}");

            if (fundingResponse?.Data == null || fundingResponse.Data.Count == 0)
                continue;

            var fundingData = fundingResponse.Data[0];

            // 6. Ищем тикер для этого инструмента
            if (!tickerDict.TryGetValue(instrument.InstId, out var ticker))
                continue;

            // 7. Создаем нормализованный объект
            var normalizedSymbol = _symbolNormalizer.Normalize(instrument.InstId, ExchangeType);
            var parsedSymbol = _symbolNormalizer.Parse(instrument.InstId, ExchangeType);

            var rate = new CurrentFundingRate
            {
                Exchange = ExchangeType.OKX,
                NormalizedSymbol = normalizedSymbol,
                BaseAsset = parsedSymbol.Base,
                QuoteAsset = parsedSymbol.Quote,
                FundingRate = SafeParseDecimal(fundingData.FundingRate),
                NextFundingTime = DateTimeOffset.FromUnixTimeMilliseconds(
                    long.Parse(fundingData.NextFundingTime)).UtcDateTime,
                MarkPrice = SafeParseDecimal(ticker.MarkPx),
                IndexPrice = SafeParseDecimal(ticker.IdxPx),
                LastCheck = DateTime.UtcNow,
                IsActive = instrument.State == "live",
                FundingIntervalHours = 8
            };

            results.Add(rate);

            // Соблюдаем rate limit (20 запросов в секунду)
            await Task.Delay(100); // 50ms между запросами
        }

        return results;
    }

    public override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            await GetAsync<object>("/", cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<OkxInstrument>> GetAllInstrumentsAsync()
    {
        try
        {
            var response = await GetAsync<OkxInstrumentsResponse>("/api/v5/public/instruments?instType=SWAP");
            return response?.Data ?? new List<OkxInstrument>();
        }
        catch (Exception ex)
        {
            return new List<OkxInstrument>();
        }
    }

    private class OkxTickersResponse
    {
        public List<OkxTicker> Data { get; set; } = new();
    }

    private class OkxTicker
    {
        public string InstId { get; set; } = string.Empty;
        public string MarkPx { get; set; } = string.Empty;
        public string IdxPx { get; set; } = string.Empty;
    }

    private class OkxFundingRateResponse
    {
        public List<OkxFundingRate> Data { get; set; } = new();
    }

    private class OkxFundingRate
    {
        public string FundingRate { get; set; } = string.Empty;
        public string NextFundingTime { get; set; } = string.Empty;
    }

    private class OkxInstrumentsResponse
    {
        public List<OkxInstrument> Data { get; set; } = new();
    }

    private class OkxInstrument
    {
        public string InstType { get; set; } = string.Empty;
        public string InstId { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }
}