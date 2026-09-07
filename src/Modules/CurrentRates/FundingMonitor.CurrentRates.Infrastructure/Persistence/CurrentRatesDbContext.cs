using FundingMonitor.CurrentRates.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.CurrentRates.Infrastructure.Persistence;

public sealed class CurrentRatesDbContext(
    DbContextOptions<CurrentRatesDbContext> options) : DbContext(options)
{
    internal DbSet<CurrentFundingRateDb> CurrentFundingRates => Set<CurrentFundingRateDb>();
    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("current_rates");

        modelBuilder.Entity<CurrentFundingRateDb>(entity =>
        {
            entity.ToTable("CurrentFundingRates");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.NormalizedSymbol, item.Exchange }).IsUnique();
            entity.HasIndex(item => new { item.IsActive, item.BaseAsset });
            entity.Property(item => item.Exchange).HasMaxLength(32);
            entity.Property(item => item.NormalizedSymbol).HasMaxLength(64);
            entity.Property(item => item.BaseAsset).HasMaxLength(32);
            entity.Property(item => item.QuoteAsset).HasMaxLength(32);
            entity.Property(item => item.MarkPrice).HasPrecision(28, 12);
            entity.Property(item => item.IndexPrice).HasPrecision(28, 12);
            entity.Property(item => item.FundingRate).HasPrecision(20, 12);
            entity.Property(item => item.PredictedNextRate).HasPrecision(20, 12);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ProcessedOnUtc, item.OccurredOnUtc });
            entity.HasIndex(item => new { item.ProcessedOnUtc, item.LockedUntilUtc });
            entity.Property(item => item.Type).HasMaxLength(512);
            entity.Property(item => item.Error).HasMaxLength(1000);
            entity.Property(item => item.LockOwner).HasMaxLength(128);
            entity.Property(item => item.Payload).HasColumnType("jsonb");
        });
    }
}
