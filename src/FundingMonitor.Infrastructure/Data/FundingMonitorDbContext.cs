using FundingMonitor.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.Infrastructure.Data;

public class FundingMonitorDbContext : DbContext
{
    public FundingMonitorDbContext(DbContextOptions<FundingMonitorDbContext> options) : base(options)
    {
    }

    public DbSet<CurrentFundingRateEntity> CurrentFundingRate { get; set; }
    public DbSet<HistoricalFundingRateEntity> HistoricalFundingRate { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CurrentFundingRateEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.NormalizedSymbol, e.Exchange })
                .IsUnique();
            entity.HasIndex(e => e.NormalizedSymbol);
            entity.HasIndex(e => e.Exchange);
            entity.HasIndex(e => new { e.BaseAsset, e.IsActive });
        });

        modelBuilder.Entity<HistoricalFundingRateEntity>(entity =>
        {
            entity.HasKey(e => new { e.Exchange, e.NormalizedSymbol, e.FundingTime });

            // Настраиваем Id как GENERATED ALWAYS AS IDENTITY для PostgreSQL
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .UseIdentityAlwaysColumn();

            entity.HasIndex(e => new { e.Exchange, e.NormalizedSymbol })
                .HasDatabaseName("IX_HistoricalFundingRate_Exchange_Symbol");

            entity.HasIndex(e => e.FundingTime)
                .HasDatabaseName("IX_HistoricalFundingRate_Time");
        });
    }
}