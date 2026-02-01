using FundingMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FundingMonitor.Data.Configurations;

public class TradingPairConfiguration : IEntityTypeConfiguration<TradingPair>
{
    public void Configure(EntityTypeBuilder<TradingPair> builder)
    {
        builder.ToTable("trading_pairs");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Symbol)
            .IsRequired()
            .HasMaxLength(20);
        
        builder.Property(p => p.BaseAsset)
            .HasMaxLength(10);
        
        builder.Property(p => p.QuoteAsset)
            .HasMaxLength(10);
        
        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);
        
        // Связи
        builder.HasOne(p => p.Exchange)
            .WithMany(e => e.TradingPairs)
            .HasForeignKey(p => p.ExchangeId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Индексы
        builder.HasIndex(p => new { p.ExchangeId, p.Symbol })
            .IsUnique();
    }
}