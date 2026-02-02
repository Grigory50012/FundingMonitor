namespace FundingMonitor.Core.Models;

// Модель для информации о торговой паре
public class TradingPairInfo
{
    public string Symbol { get; set; } = string.Empty;           // BTCUSDT
    public string BaseAsset { get; set; } = string.Empty;       // BTC
    public string QuoteAsset { get; set; } = string.Empty;      // USDT
    public decimal Price { get; set; }
    public decimal Volume24h { get; set; }
    public decimal FundingRate { get; set; }                    // Текущая ставка
    public DateTime? NextFundingTime { get; set; }
    public decimal? PredictedFundingRate { get; set; }
    public decimal MarkPrice { get; set; }
    public decimal IndexPrice { get; set; }
}