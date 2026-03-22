using FundingMonitor.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.Infrastructure.Data;

public class FundingMonitorDbContext : DbContext
{
    public FundingMonitorDbContext(DbContextOptions<FundingMonitorDbContext> options) : base(options)
    {
    }

    public DbSet<CurrentFundingRateDb> CurrentFundingRate { get; set; }
    public DbSet<HistoricalFundingRateDb> HistoricalFundingRate { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CurrentFundingRateDb>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.NormalizedSymbol, e.Exchange })
                .IsUnique();
            entity.HasIndex(e => new { e.IsActive, e.BaseAsset });
        });

        modelBuilder.Entity<HistoricalFundingRateDb>(entity =>
        {
            entity.HasKey(e => new { e.Exchange, e.NormalizedSymbol, e.FundingTime });

            entity.HasIndex(e => new { e.Exchange, e.NormalizedSymbol })
                .HasDatabaseName("IX_HistoricalFundingRate_Exchange_Symbol");

            entity.HasIndex(e => e.FundingTime)
                .HasDatabaseName("IX_HistoricalFundingRate_Time");
        });
    }
}