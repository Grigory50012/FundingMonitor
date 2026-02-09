using System.Diagnostics;
using FundingMonitor.Core.Enums;
using FundingMonitor.Core.Interfaces;
using FundingMonitor.Core.Models;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Core.Services;

public class FundingDataService : IFundingDataService
{
    private readonly IEnumerable<IExchangeApiClient> _exchangeClients;
    private readonly ILogger _logger;
    
    public FundingDataService(
        IEnumerable<IExchangeApiClient> exchangeClients,
        ILogger<FundingDataService> logger)
    {
        _exchangeClients = exchangeClients;
        _logger = logger;
    }
    
    public async Task<List<NormalizedFundingRate>> CollectAllRatesAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Начало параллельного сбора данных с {Count} бирж", 
            _exchangeClients.Count());
        
        try
        {
            // Создаем задачи для всех бирж
            var collectionTasks = _exchangeClients
                .Select(client => CollectFromExchangeAsync(client, cancellationToken))
                .ToList();
            
            // Ждем завершения ВСЕХ задач, даже с ошибками
            var results = await Task.WhenAll(collectionTasks);
            
            stopwatch.Stop();
            
            // Анализ результатов
            var successful = results.Count(r => r.Success);
            var failed = results.Count(r => !r.Success);
            var totalRates = results.SelectMany(r => r.Rates).Count();
        
            if (failed > 0)
            {
                _logger.LogWarning("Сбор завершен с ошибками: {Successful}/{Total} успешно, {Failed} с ошибками",
                    successful, _exchangeClients.Count(), failed);
            }
        
            _logger.LogInformation("""
                                   Параллельная сборка завершена за {Time}ms
                                   Успешно собраны данные с: {Successful}/{Total} бирж
                                   Сумма собранных пар: {TotalRates}
                                   """,
                stopwatch.ElapsedMilliseconds,
                successful,
                _exchangeClients.Count(),
                totalRates);

            return results.SelectMany(r => r.Rates).ToList();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Критическая ошибка при сборе данных");
            throw;
        }
    }
    
    private async Task<(ExchangeType ExchangeType, List<NormalizedFundingRate> Rates, bool Success)> 
        CollectFromExchangeAsync(IExchangeApiClient client, CancellationToken cancellationToken)
    {
        try
        {
            if (client.IsRateLimited)
            {
                _logger.LogWarning("[{Exchange}] Пропущено из-за ограничения скорости запросов", 
                    client.ExchangeType);
                return (client.ExchangeType, new List<NormalizedFundingRate>(), false);
            }
        
            _logger.LogDebug("[{Exchange}] Начало сбора данных...", client.ExchangeType);
        
            // Таймаут для конкретной биржи
            using var perExchangeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
            var rates = await client.GetAllFundingRatesAsync(perExchangeCts.Token);
        
            return (client.ExchangeType, rates, true);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[{Exchange}] Таймаут сбора данных", client.ExchangeType);
            return (client.ExchangeType, new List<NormalizedFundingRate>(), false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Exchange}] Не удалось собрать данные", client.ExchangeType);
            return (client.ExchangeType, new List<NormalizedFundingRate>(), false);
        }
    }
    
    public List<ArbitrageOpportunity> FindArbitrageOpportunitiesAsync(List<NormalizedFundingRate> processinRates)
    {
        var groupedRates = processinRates
            .GroupBy(r => r.NormalizedSymbol)
            .Where(g => g.Count() >= 2) // Нужно минимум 2 биржи
            .ToList();
        
        var opportunities = new List<ArbitrageOpportunity>();
        
        foreach (var group in groupedRates)
        {
            var rates = group.ToList();
            var minRate = rates.Min(r => r.FundingRate);
            var maxRate = rates.Max(r => r.FundingRate);
            var difference = maxRate - minRate;
            
            if (difference > 0.0001m) // Минимальная разница 0.01%
            {
                opportunities.Add(new ArbitrageOpportunity
                {
                    Symbol = group.Key,
                    Rates = rates
                });
            }
        }
        
        _logger.LogInformation("Found {Count} arbitrage opportunities", opportunities.Count);
        return opportunities.OrderByDescending(o => o.MaxDifference).ToList();
    }
    
    public async Task<Dictionary<ExchangeType, bool>> CheckExchangesStatusAsync()
    {
        var status = new Dictionary<ExchangeType, bool>();
        var tasks = _exchangeClients.ToDictionary(
            client => client.ExchangeType,
            client => client.IsAvailableAsync());
        
        await Task.WhenAll(tasks.Values);
        
        foreach (var (exchangeType, task) in tasks)
        {
            status[exchangeType] = await task;
        }
        
        return status;
    }
}