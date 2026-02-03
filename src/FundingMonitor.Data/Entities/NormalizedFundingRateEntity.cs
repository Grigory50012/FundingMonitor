using System.ComponentModel.DataAnnotations.Schema;

namespace FundingMonitor.Data.Entities;

[Table("normalized_funding_rates")]
public class NormalizedFundingRateEntity
{
    public int Id { get; set; }
    
    // Идентификаторы
    public string Exchange { get; set; } = string.Empty;
    public string NormalizedSymbol { get; set; } = string.Empty;
    public string BaseAsset { get; set; } = string.Empty;
    public string QuoteAsset { get; set; } = string.Empty;
    public string OriginalSymbol { get; set; } = string.Empty;
    
    // Данные
    [Column(TypeName = "decimal(10,8)")]
    public decimal FundingRate { get; set; }
    
    [Column(TypeName = "decimal(10,8)")]
    public decimal? PredictedNextRate { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal? MarkPrice { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal? IndexPrice { get; set; }
    
    // Временные метки
    public DateTime NextFundingTime { get; set; }
    public DateTime DataTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Метаданные
    public string? InstrumentType { get; set; }
    public bool IsActive { get; set; } = true;
}