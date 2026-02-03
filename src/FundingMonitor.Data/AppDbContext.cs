using FundingMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.Data;

public class AppDbContext : DbContext
{
    public DbSet<NormalizedFundingRateEntity> FundingRates { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NormalizedFundingRateEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.NormalizedSymbol, e.Exchange, e.DataTime });
            entity.HasIndex(e => e.NormalizedSymbol);
            entity.HasIndex(e => e.Exchange);
            entity.HasIndex(e => e.DataTime);
        });
    }
}