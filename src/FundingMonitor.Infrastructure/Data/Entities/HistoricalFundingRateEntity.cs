using System.ComponentModel.DataAnnotations.Schema;

namespace FundingMonitor.Infrastructure.Data.Entities;

[Table("HistoricalFundingRate")]
public class HistoricalFundingRateEntity
{
    public int Id { get; set; }

    // Идентификаторы
    public string Exchange { get; set; } = string.Empty;
    public string NormalizedSymbol { get; set; } = string.Empty;

    // Данные
    [Column(TypeName = "decimal(10,8)")] public decimal FundingRate { get; set; }

    public DateTime FundingTime { get; set; }
    public DateTime CollectedAt { get; set; }
}