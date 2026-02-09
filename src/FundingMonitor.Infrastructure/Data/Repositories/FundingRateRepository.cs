using FundingMonitor.Application.Interfaces.Repositories;
using FundingMonitor.Core.Entities;
using FundingMonitor.Infrastructure.Data.Entities;
using FundingMonitor.Infrastructure.Data.Mappers;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.Infrastructure.Data.Repositories;

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
        
        // Получаем ключи для поиска существующих записей
        var keys = rateList
            .Select(r => new { Exchange = r.Exchange.ToString(), r.NormalizedSymbol })
            .Distinct()
            .ToList();
        
        // Ищем существующие записи одним запросом
        var existingEntities = await _context.FundingRateCurrent
            .Where(r => keys.Select(k => k.Exchange).Contains(r.Exchange) &&
                        keys.Select(k => k.NormalizedSymbol).Contains(r.NormalizedSymbol))
            .ToDictionaryAsync(r => new { r.Exchange, r.NormalizedSymbol });
        
        var entitiesToAdd = new List<NormalizedFundingRateEntity>();
        var entitiesToUpdate = new List<NormalizedFundingRateEntity>();
        
        foreach (var rate in rateList)
        {
            var key = new { Exchange = rate.Exchange.ToString(), rate.NormalizedSymbol };
            
            if (existingEntities.TryGetValue(key, out var existingEntity))
            {
                // Обновляем существующую запись
                FundingRateMapper.UpdateEntity(existingEntity, rate);
                entitiesToUpdate.Add(existingEntity);
            }
            else
            {
                // Создаем новую запись
                var entity = FundingRateMapper.ToEntity(rate);
                entitiesToAdd.Add(entity);
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
        
        query = query.OrderBy(r => r.NormalizedSymbol)
            .ThenBy(r => r.Exchange);
    
        return await query.ToDomainListAsync();
    }
}