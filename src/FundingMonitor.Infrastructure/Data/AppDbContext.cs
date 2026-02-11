using FundingMonitor.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<NormalizedFundingRateEntity> FundingRateCurrent { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NormalizedFundingRateEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.NormalizedSymbol, e.Exchange })
                .IsUnique();
            entity.HasIndex(e => e.NormalizedSymbol);
            entity.HasIndex(e => e.Exchange);
            entity.HasIndex(e => new { e.BaseAsset, e.IsActive });
        });
    }
}