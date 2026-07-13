using System.ComponentModel.DataAnnotations.Schema;

namespace FundingMonitor.Infrastructure.Data.Entities;

[Table("CurrentFundingRate")]
public class CurrentFundingRateDb
{
    public int Id { get; set; }

    // Идентификаторы
    public string Exchange { get; set; } = string.Empty;
    public string NormalizedSymbol { get; set; } = string.Empty;
    public string BaseAsset { get; set; } = string.Empty;
    public string QuoteAsset { get; set; } = string.Empty;

    // Данные
    [Column(TypeName = "decimal(18,8)")] public decimal? MarkPrice { get; set; }

    [Column(TypeName = "decimal(18,8)")] public decimal? IndexPrice { get; set; }

    [Column(TypeName = "decimal(10,8)")] public decimal FundingRate { get; set; }

    public int FundingIntervalHours { get; set; }
    public DateTime? NextFundingTime { get; set; }

    // LastSeenAt обновляется только когда биржа реально вернула эту пару.
    public DateTime LastSeenAt { get; set; }

    [Column(TypeName = "decimal(10,8)")] public decimal? PredictedNextRate { get; set; }

    // IsActive=false означает, что пара давно не встречалась в успешных snapshot и скрыта из API/arbitrage.
    public bool IsActive { get; set; } = true;
}
