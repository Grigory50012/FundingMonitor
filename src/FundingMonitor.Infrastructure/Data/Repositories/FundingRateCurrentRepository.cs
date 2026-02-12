using EFCore.BulkExtensions;
using FundingMonitor.Application.Interfaces.Repositories;
using FundingMonitor.Core.Entities;
using FundingMonitor.Infrastructure.Data.Mappers;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.Infrastructure.Data.Repositories;

public class FundingRateCurrentRepository : IFundingRateCurrentRepository
{
    private readonly AppDbContext _context;

    public FundingRateCurrentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task UpdateRatesAsync(IEnumerable<CurrentFundingRate> rates)
    {
        var rateList = rates.ToList();
        if (rateList.Count == 0) return;

        // 1. Маппинг и Upsert
        var entities = rateList.Select(FundingRateMapper.ToEntity).ToList();
        var bulkConfig = new BulkConfig
        {
            UpdateByProperties = new List<string> { "Exchange", "NormalizedSymbol" },
            TrackingEntities = false,
            BatchSize = 4000
        };

        await _context.BulkInsertOrUpdateAsync(entities, bulkConfig);

        // 2. Получаем все уникальные ключи из БД (только Exchange и NormalizedSymbol)
        var existingKeys = await _context.CurrentFundingRate
            .Select(r => new { r.Exchange, r.NormalizedSymbol })
            .ToListAsync();

        // 3. Множество ключей, которые должны остаться (из входящего списка)
        var incomingKeys = entities
            .Select(e => new { e.Exchange, e.NormalizedSymbol })
            .ToHashSet();

        // 4. Ключи, которые есть в БД, но отсутствуют во входящем списке → подлежат удалению
        var keysToDelete = existingKeys
            .Where(k => !incomingKeys.Contains(k))
            .ToList();

        if (keysToDelete.Count != 0)
        {
            // 5. BulkDelete по ключам (можно удалять через поиск по составному ключу)
            var exchangesToDelete = keysToDelete.Select(k => k.Exchange).ToList();
            var symbolsToDelete = keysToDelete.Select(k => k.NormalizedSymbol).ToList();

            var entitiesToDelete = await _context.CurrentFundingRate
                .Where(r => exchangesToDelete.Contains(r.Exchange) &&
                            symbolsToDelete.Contains(r.NormalizedSymbol))
                .ToListAsync();

            // Дополнительная фильтрация в памяти для точного соответствия пар
            entitiesToDelete = entitiesToDelete
                .Where(e => keysToDelete.Any(k =>
                    k.Exchange == e.Exchange &&
                    k.NormalizedSymbol == e.NormalizedSymbol))
                .ToList();

            await _context.BulkDeleteAsync(entitiesToDelete);
        }
    }

    public async Task<IEnumerable<CurrentFundingRate>> GetRatesAsync(
        string? symbol, List<ExchangeType>? exchanges)
    {
        var query = _context.CurrentFundingRate
            .AsQueryable()
            .AsQueryable();

        // Опциональный фильтр по exchanges
        if (exchanges != null && exchanges.Count != 0)
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