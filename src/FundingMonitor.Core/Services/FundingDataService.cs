using FundingMonitor.Core.Interfaces;
using FundingMonitor.Core.Models;
using FundingMonitor.Data;
using FundingMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Core.Services;

public class FundingDataService : IFundingDataService
{
    private readonly BinanceApiClient _binanceClient;
    private readonly BybitApiClient _bybitClient;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<FundingDataService> _logger;
    
    public FundingDataService(
        BinanceApiClient binanceClient,
        BybitApiClient bybitClient,
        AppDbContext dbContext,
        ILogger<FundingDataService> logger)
    {
        _binanceClient = binanceClient;
        _bybitClient = bybitClient;
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<List<FundingRateComparison>> CompareFundingRatesAsync(List<string> symbols = null)
    {
        _logger.LogInformation("Starting funding rates comparison");
        
        var comparisons = new List<FundingRateComparison>();
        
        // Если символы не указаны, берем общие пары из БД
        if (symbols == null || symbols.Count == 0)
        {
            symbols = await GetCommonPairsFromDatabaseAsync();
            _logger.LogInformation("Using {Count} common pairs from database", symbols.Count);
        }
        
        // Ограничим количество для тестирования
        var symbolsToCheck = symbols.Take(10).ToList();
        
        foreach (var symbol in symbolsToCheck)
        {
            try
            {
                _logger.LogDebug("Comparing rates for {Symbol}", symbol);
                
                // Получаем ставки параллельно для скорости
                var binanceTask = _binanceClient.GetCurrentFundingRateAsync(symbol);
                var bybitTask = _bybitClient.GetCurrentFundingRateAsync(symbol);
                
                await Task.WhenAll(binanceTask, bybitTask);
                
                var binanceRate = await binanceTask;
                var bybitRate = await bybitTask;
                
                // Сохраняем в БД
                await SaveFundingRateToDatabaseAsync(symbol, "Binance", binanceRate);
                await SaveFundingRateToDatabaseAsync(symbol, "Bybit", bybitRate);
                
                // Создаем сравнение
                var comparison = new FundingRateComparison
                {
                    Symbol = symbol,
                    BinanceRate = binanceRate,
                    BybitRate = bybitRate
                };
                
                // Добавляем только если разница значительная (>0.01%)
                if (comparison.Difference > 0.0001m)
                {
                    comparisons.Add(comparison);
                    _logger.LogInformation("Found opportunity: {Symbol} diff={Difference:P4}", 
                        symbol, comparison.Difference);
                }
                
                // Небольшая пауза чтобы не превысить rate limits
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error comparing rates for {Symbol}", symbol);
            }
        }
        
        _logger.LogInformation("Comparison completed. Found {Count} opportunities", comparisons.Count);
        
        return comparisons.OrderByDescending(c => c.Difference).ToList();
    }
    
    public async Task UpdateDatabaseFromExchangesAsync()
    {
        _logger.LogInformation("Updating database from exchanges");
        
        try
        {
            // 1. Обновляем список пар для каждой биржи
            await UpdateTradingPairsAsync("Binance", _binanceClient);
            await UpdateTradingPairsAsync("Bybit", _bybitClient);
            
            // 2. Получаем и сравниваем ставки для общих пар
            var commonPairs = await GetCommonPairsFromDatabaseAsync();
            await CompareFundingRatesAsync(commonPairs);
            
            _logger.LogInformation("Database update completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating database from exchanges");
            throw;
        }
    }
    
    public async Task<List<TradingPairInfo>> GetAvailablePairsFromExchangeAsync(string exchangeName)
    {
        throw new NotImplementedException();
        //    var client = exchangeName.ToLower() switch
        //    {
        //        "binance" => _binanceClient,
        //        "bybit" => _bybitClient,
        //        _ => throw new ArgumentException($"Unknown exchange: {exchangeName}")
        //    };
        //    
        //    return await client.GetAvailablePairsAsync();
    }
    
    private async Task<List<string>> GetCommonPairsFromDatabaseAsync()
    {
        // Находим пары, которые есть на обеих биржах
        var binancePairs = await _dbContext.TradingPairs
            .Where(p => p.Exchange.Name == "Binance" && p.IsActive)
            .Select(p => p.Symbol)
            .ToListAsync();
            
        var bybitPairs = await _dbContext.TradingPairs
            .Where(p => p.Exchange.Name == "Bybit" && p.IsActive)
            .Select(p => p.Symbol)
            .ToListAsync();
        
        // Находим пересечение
        var commonPairs = binancePairs.Intersect(bybitPairs).ToList();
        
        return commonPairs;
    }
    
    private async Task UpdateTradingPairsAsync(string exchangeName, IExchangeApiClient client)
    {
        _logger.LogInformation("Updating trading pairs for {Exchange}", exchangeName);
        
        var exchange = await _dbContext.Exchanges
            .FirstOrDefaultAsync(e => e.Name == exchangeName);
            
        if (exchange == null)
        {
            _logger.LogWarning("Exchange {Exchange} not found in database", exchangeName);
            return;
        }
        
        var pairsFromApi = await client.GetAvailablePairsAsync();
        
        foreach (var pairInfo in pairsFromApi)
        {
            var existingPair = await _dbContext.TradingPairs
                .FirstOrDefaultAsync(p => p.ExchangeId == exchange.Id 
                                          && p.Symbol == pairInfo.Symbol);
            
            if (existingPair == null)
            {
                // Новая пара
                var newPair = new TradingPair
                {
                    Symbol = pairInfo.Symbol,
                    BaseAsset = pairInfo.BaseAsset,
                    QuoteAsset = pairInfo.QuoteAsset,
                    ExchangeId = exchange.Id,
                    ListedAt = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow,
                    IsActive = true
                };
                
                _dbContext.TradingPairs.Add(newPair);
                _logger.LogDebug("Added new pair: {Exchange} {Symbol}", exchangeName, pairInfo.Symbol);
            }
            else
            {
                // Обновляем время последнего визита
                existingPair.LastSeen = DateTime.UtcNow;
                existingPair.IsActive = true;
                
                // Обновляем базовый актив если изменился
                if (!string.IsNullOrEmpty(pairInfo.BaseAsset) && existingPair.BaseAsset != pairInfo.BaseAsset)
                {
                    existingPair.BaseAsset = pairInfo.BaseAsset;
                }
                
                _logger.LogDebug("Updated pair: {Exchange} {Symbol}", exchangeName, pairInfo.Symbol);
            }
        }
        
        // Деактивируем пары которые больше не доступны
        var activeSymbols = pairsFromApi.Select(p => p.Symbol).ToList();
        var inactivePairs = await _dbContext.TradingPairs
            .Where(p => p.ExchangeId == exchange.Id 
                        && !activeSymbols.Contains(p.Symbol))
            .ToListAsync();
            
        foreach (var pair in inactivePairs)
        {
            pair.IsActive = false;
            _logger.LogDebug("Deactivated pair: {Exchange} {Symbol}", exchangeName, pair.Symbol);
        }
        
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Updated {Count} pairs for {Exchange}", pairsFromApi.Count, exchangeName);
    }
    
    private async Task SaveFundingRateToDatabaseAsync(string symbol, string exchangeName, FundingRateInfo rateInfo)
    {
        var exchange = await _dbContext.Exchanges
            .FirstOrDefaultAsync(e => e.Name == exchangeName);
            
        var pair = await _dbContext.TradingPairs
            .FirstOrDefaultAsync(p => p.ExchangeId == exchange!.Id && p.Symbol == symbol);
        
        if (exchange == null || pair == null)
        {
            _logger.LogWarning("Cannot save funding rate: {Exchange} {Symbol} not found", exchangeName, symbol);
            return;
        }
        
        // Проверяем, есть ли уже запись для этого времени выплаты
        var existingRate = await _dbContext.FundingRates
            .FirstOrDefaultAsync(f => 
                f.ExchangeId == exchange.Id && 
                f.PairId == pair.Id && 
                f.FundingTime == rateInfo.NextFundingTime);
        
        if (existingRate == null)
        {
            var fundingRate = new FundingRate
            {
                PairId = pair.Id,
                ExchangeId = exchange.Id,
                Rate = rateInfo.Rate,
                FundingTime = rateInfo.NextFundingTime,
                PredictedRate = rateInfo.PredictedRate,
                MarkPrice = rateInfo.MarkPrice,
                IndexPrice = rateInfo.IndexPrice,
                CreatedAt = DateTime.UtcNow
            };
            
            _dbContext.FundingRates.Add(fundingRate);
            await _dbContext.SaveChangesAsync();
            
            _logger.LogDebug("Saved funding rate: {Exchange} {Symbol} {Rate:P6}", 
                exchangeName, symbol, rateInfo.Rate);
        }
    }
}