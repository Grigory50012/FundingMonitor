using FundingMonitor.HistoricalRates.Infrastructure.Inbox;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.HistoricalRates.Infrastructure.Persistence;

public sealed class HistoricalRatesDbContext(
    DbContextOptions<HistoricalRatesDbContext> options) : DbContext(options)
{
    internal DbSet<HistoricalFundingRateDb> HistoricalFundingRates => Set<HistoricalFundingRateDb>();
    internal DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("historical_rates");
        modelBuilder.Entity<HistoricalFundingRateDb>(entity =>
        {
            entity.ToTable("HistoricalFundingRates");
            entity.HasKey(item => new { item.Exchange, item.NormalizedSymbol, item.FundingTime });
            entity.HasIndex(item => new { item.Exchange, item.NormalizedSymbol });
            entity.HasIndex(item => item.FundingTime);
            entity.Property(item => item.Exchange).HasMaxLength(32);
            entity.Property(item => item.NormalizedSymbol).HasMaxLength(64);
            entity.Property(item => item.FundingRate).HasPrecision(20, 12);
        });
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("InboxMessages");
            entity.HasKey(item => new { item.EventId, item.Consumer });
            entity.Property(item => item.Consumer).HasMaxLength(256);
        });
    }
}
