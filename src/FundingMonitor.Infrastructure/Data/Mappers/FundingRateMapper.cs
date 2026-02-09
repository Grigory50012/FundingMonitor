using FundingMonitor.Core.Entities;
using FundingMonitor.Infrastructure.Data.Entities;

namespace FundingMonitor.Infrastructure.Data.Mappers;

public static class FundingRateMapper
{
    public static NormalizedFundingRateEntity ToEntity(NormalizedFundingRate domainModel)
    {
        if (domainModel == null)
            throw new ArgumentNullException(nameof(domainModel), "Domain model cannot be null");
        
        ValidateDomainModel(domainModel);

        return new NormalizedFundingRateEntity
        {
            Id = 0, // Будет сгенерирован БД
            Exchange = domainModel.Exchange.ToString(),
            NormalizedSymbol = domainModel.NormalizedSymbol ?? 
                               throw new ArgumentException("NormalizedSymbol cannot be null", nameof(domainModel)),
            BaseAsset = domainModel.BaseAsset,
            QuoteAsset = domainModel.QuoteAsset,
            MarkPrice = domainModel.MarkPrice,
            IndexPrice = domainModel.IndexPrice,
            FundingRate = domainModel.FundingRate,
            FundingIntervalHours = domainModel.FundingIntervalHours ?? GetDefaultFundingInterval(domainModel.Exchange),
            NextFundingTime = domainModel.NextFundingTime,
            LastCheck = domainModel.LastCheck,
            PredictedNextRate = domainModel.PredictedNextRate,
            IsActive = domainModel.IsActive
        };
    }
    
    public static NormalizedFundingRate ToDomain(NormalizedFundingRateEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), "Entity cannot be null");
        
        ValidateEntity(entity);

        return new NormalizedFundingRate
        {
            Exchange = ParseExchange(entity.Exchange),
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
    
    public static void UpdateEntity(NormalizedFundingRateEntity entity, NormalizedFundingRate domainModel)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), "Entity cannot be null");
        
        if (domainModel == null)
            throw new ArgumentNullException(nameof(domainModel), "Domain model cannot be null");
        
        ValidateDomainModel(domainModel);

        // Проверяем, что обновляем правильную запись
        if (entity.Exchange != domainModel.Exchange.ToString() || 
            entity.NormalizedSymbol != domainModel.NormalizedSymbol)
        {
            throw new InvalidOperationException(
                $"Cannot update entity. Exchange/Symbol mismatch. " +
                $"Entity: {entity.Exchange}/{entity.NormalizedSymbol}, " +
                $"Domain: {domainModel.Exchange}/{domainModel.NormalizedSymbol}");
        }

        entity.MarkPrice = domainModel.MarkPrice;
        entity.IndexPrice = domainModel.IndexPrice;
        entity.FundingRate = domainModel.FundingRate;
        entity.FundingIntervalHours = domainModel.FundingIntervalHours ?? GetDefaultFundingInterval(domainModel.Exchange);
        entity.NextFundingTime = domainModel.NextFundingTime;
        entity.LastCheck = domainModel.LastCheck;
        entity.PredictedNextRate = domainModel.PredictedNextRate;
        entity.IsActive = domainModel.IsActive;
    }
    
    public static IReadOnlyList<NormalizedFundingRate> ToDomainList(
        IEnumerable<NormalizedFundingRateEntity> entities)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));
        
        return entities
            .Select(ToDomain)
            .ToList()
            .AsReadOnly();
    }
    
    public static IReadOnlyList<NormalizedFundingRateEntity> ToEntityList(
        IEnumerable<NormalizedFundingRate> domainModels)
    {
        if (domainModels == null)
            throw new ArgumentNullException(nameof(domainModels));
        
        return domainModels
            .Select(ToEntity)
            .ToList()
            .AsReadOnly();
    }
    
    public static NormalizedFundingRateEntity ToEntityOrUpdate(
        NormalizedFundingRate domainModel, 
        NormalizedFundingRateEntity? existingEntity = null)
    {
        if (domainModel == null)
            throw new ArgumentNullException(nameof(domainModel));
        
        return existingEntity == null 
            ? ToEntity(domainModel) 
            : UpdateAndReturnEntity(existingEntity, domainModel);
    }
    
    public static List<NormalizedFundingRate> ToDomainListFast(
        List<NormalizedFundingRateEntity>? entities)
    {
        if (entities is null || entities.Count is 0)
            return new List<NormalizedFundingRate>();

        var result = new List<NormalizedFundingRate>(entities.Count);
        
        foreach (var entity in entities)
        {
            result.Add(new NormalizedFundingRate
            {
                Exchange = ParseExchange(entity.Exchange),
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
            });
        }
        
        return result;
    }
    
    private static ExchangeType ParseExchange(string exchangeName)
    {
        if (string.IsNullOrWhiteSpace(exchangeName))
            throw new ArgumentException("Exchange name cannot be empty", nameof(exchangeName));
        
        return Enum.TryParse<ExchangeType>(exchangeName, true, out var result)
            ? result
            : throw new InvalidOperationException($"Unknown exchange: '{exchangeName}'. " +
                                                  $"Valid values: {string.Join(", ", Enum.GetNames<ExchangeType>())}");
    }
    
    private static int GetDefaultFundingInterval(ExchangeType exchange)
    {
        return exchange switch
        {
            ExchangeType.Binance => 8,
            ExchangeType.Bybit => 8,
            ExchangeType.OKX => 8,
            _ => 8 // По умолчанию 8 часов
        };
    }
    
    private static void ValidateDomainModel(NormalizedFundingRate domainModel)
    {
        if (string.IsNullOrWhiteSpace(domainModel.NormalizedSymbol))
            throw new ArgumentException("NormalizedSymbol is required", nameof(domainModel));
    }
    
    private static void ValidateEntity(NormalizedFundingRateEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.NormalizedSymbol))
            throw new ArgumentException($"Entity has invalid NormalizedSymbol: {entity.NormalizedSymbol}", 
                nameof(entity));
        
        if (string.IsNullOrWhiteSpace(entity.Exchange))
            throw new ArgumentException($"Entity has invalid Exchange: {entity.Exchange}", 
                nameof(entity));
        
        if (entity.FundingIntervalHours <= 0 || entity.FundingIntervalHours > 24)
            throw new ArgumentException($"Invalid FundingIntervalHours: {entity.FundingIntervalHours}", 
                nameof(entity));
    }
    
    private static NormalizedFundingRateEntity UpdateAndReturnEntity(
        NormalizedFundingRateEntity entity, 
        NormalizedFundingRate domainModel)
    {
        UpdateEntity(entity, domainModel);
        return entity;
    }
}