using FundingMonitor.Core.Models;
using FundingMonitor.Data.Entities;

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
        var entities = rates.Select(r => new NormalizedFundingRateEntity
        {
            Exchange = r.Exchange.ToString(),
            NormalizedSymbol = r.NormalizedSymbol,
            BaseAsset = r.BaseAsset,
            QuoteAsset = r.QuoteAsset,
            OriginalSymbol = r.OriginalSymbol,
            FundingRate = r.FundingRate,
            PredictedNextRate = r.PredictedNextRate,
            MarkPrice = r.MarkPrice,
            IndexPrice = r.IndexPrice,
            NextFundingTime = r.NextFundingTime,
            DataTime = r.DataTime,
            InstrumentType = r.InstrumentType,
            IsActive = r.IsActive
        }).ToList();
        
        _context.FundingRates.AddRange(entities);
        await _context.SaveChangesAsync();
    }
}