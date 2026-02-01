using FundingMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FundingMonitor.Data.Configurations;

public class FundingRateConfiguration : IEntityTypeConfiguration<FundingRate>
{
    public void Configure(EntityTypeBuilder<FundingRate> builder)
    {
        builder.ToTable("funding_rates");
        
        builder.HasKey(f => f.Id);
        
        builder.Property(f => f.Rate)
            .HasPrecision(10, 8);  // 10 цифр всего, 8 после запятой
        
        builder.Property(f => f.PredictedRate)
            .HasPrecision(10, 8);
        
        builder.Property(f => f.MarkPrice)
            .HasPrecision(18, 8);
        
        builder.Property(f => f.IndexPrice)
            .HasPrecision(18, 8);
        
        builder.Property(f => f.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        // Связи
        builder.HasOne(f => f.Exchange)
            .WithMany(e => e.FundingRates)
            .HasForeignKey(f => f.ExchangeId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(f => f.Pair)
            .WithMany(p => p.FundingRates)
            .HasForeignKey(f => f.PairId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Индексы
        builder.HasIndex(f => f.FundingTime);
        builder.HasIndex(f => new { f.PairId, f.FundingTime });
        builder.HasIndex(f => f.CreatedAt);
    }
}