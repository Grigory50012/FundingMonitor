using FundingMonitor.Core.Enums;
using FundingMonitor.Core.Models;
using FundingMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.Data.Repositories;

public class FundingRateRepository : IFundingRateRepository
{
    private readonly AppDbContext _context;
    
    public FundingRateRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task SaveRatesAsync(IEnumerable<NormalizedFundingRate> rates)
    {
        var rateList = rates.ToList();
        if (!rateList.Any())
            return;
        
        // Получаем все существующие записи одним запросом
        var exchanges = rateList.Select(r => r.Exchange.ToString()).Distinct().ToList();
        var symbols = rateList.Select(r => r.NormalizedSymbol).Distinct().ToList();
        
        var existingEntities = await _context.FundingRateCurrent
            .Where(r => 
                exchanges.Contains(r.Exchange) && 
                symbols.Contains(r.NormalizedSymbol))
            .ToDictionaryAsync(r => new { r.Exchange, r.NormalizedSymbol });
        
        var entitiesToAdd = new List<NormalizedFundingRateEntity>();
        var entitiesToUpdate = new List<NormalizedFundingRateEntity>();
        
        foreach (var rate in rateList)
        {
            var key = new { Exchange = rate.Exchange.ToString(), rate.NormalizedSymbol };
            
            if (existingEntities.TryGetValue(key, out var existingEntity))
            {
                // Обновляем существующую
                if (!ShouldUpdateEntity(existingEntity, rate)) 
                    continue;
                
                UpdateEntity(existingEntity, rate);
                entitiesToUpdate.Add(existingEntity);
            }
            else
            {
                // Добавляем новую
                entitiesToAdd.Add(CreateEntity(rate));
            }
        }
        
        // Используем Bulk Operations для оптимизации
        if (entitiesToAdd.Any())
        {
            await _context.FundingRateCurrent.AddRangeAsync(entitiesToAdd);
        }
        
        if (entitiesToUpdate.Any())
        {
            _context.FundingRateCurrent.UpdateRange(entitiesToUpdate);
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<NormalizedFundingRate>> GetRatesAsync(
        string? symbol, List<ExchangeType>? exchanges)
    {
        var query = _context.FundingRateCurrent.AsQueryable();
    
        // Опциональный фильтр по exchanges
        if (exchanges != null && exchanges.Any())
        {
            var exchangeNames = exchanges.Select(e => e.ToString()).ToList();
            query = query.Where(r => exchangeNames.Contains(r.Exchange));
        }
    
        // Опциональный фильтр по symbol
        if (!string.IsNullOrEmpty(symbol))
        {
            query = query.Where(r => r.BaseAsset == symbol);
        }
    
        // Обязательный фильтр по активности
        query = query.Where(r => r.IsActive);
    
        var rates = await query.ToListAsync();
        return rates.Select(MapEntityToModel);
    }
    
    private NormalizedFundingRate MapEntityToModel(NormalizedFundingRateEntity entity)
    {
        return new NormalizedFundingRate
        {
            Exchange = Enum.Parse<ExchangeType>(entity.Exchange),
            NormalizedSymbol = entity.NormalizedSymbol,
            BaseAsset = entity.BaseAsset,
            QuoteAsset = entity.QuoteAsset,
            MarkPrice = entity.MarkPrice ?? 0,
            IndexPrice = entity.IndexPrice ?? 0,
            FundingRate = entity.FundingRate,
            FundingIntervalHours = entity.FundingIntervalHours,
            NextFundingTime = entity.NextFundingTime,
            LastCheck = entity.LastCheck,
            PredictedNextRate = entity.PredictedNextRate,
            IsActive = entity.IsActive
        };
    }

    private static bool ShouldUpdateEntity(NormalizedFundingRateEntity entity, NormalizedFundingRate rate)
    {
        // Обновляем только если данные изменились или прошло достаточно времени
        return entity.FundingRate != rate.FundingRate ||
               entity.MarkPrice != rate.MarkPrice ||
               entity.IndexPrice != rate.IndexPrice ||
               entity.PredictedNextRate != rate.PredictedNextRate ||
               entity.NextFundingTime != rate.NextFundingTime ||
               entity.LastCheck.AddMinutes(5) < DateTime.UtcNow; // Обновляем минимум раз в 5 минут
    }
    
    private static void UpdateEntity(NormalizedFundingRateEntity entity, NormalizedFundingRate rate)
    {
        entity.MarkPrice = rate.MarkPrice;
        entity.IndexPrice = rate.IndexPrice;
        entity.FundingRate = rate.FundingRate;
        entity.FundingIntervalHours = rate.FundingIntervalHours ?? 8;
        entity.NextFundingTime = rate.NextFundingTime;
        entity.LastCheck = rate.LastCheck;
        entity.PredictedNextRate = rate.PredictedNextRate;
        entity.IsActive = rate.IsActive;
    }
    
    private static NormalizedFundingRateEntity CreateEntity(NormalizedFundingRate rate)
    {
        return new NormalizedFundingRateEntity
        {
            Exchange = rate.Exchange.ToString(),
            NormalizedSymbol = rate.NormalizedSymbol,
            BaseAsset = rate.BaseAsset,
            QuoteAsset = rate.QuoteAsset,
            MarkPrice = rate.MarkPrice,
            IndexPrice = rate.IndexPrice,
            FundingRate = rate.FundingRate,
            FundingIntervalHours = rate.FundingIntervalHours ?? 8,
            NextFundingTime = rate.NextFundingTime,
            LastCheck = rate.LastCheck,
            PredictedNextRate = rate.PredictedNextRate,
            IsActive = rate.IsActive,
        };
    }
}