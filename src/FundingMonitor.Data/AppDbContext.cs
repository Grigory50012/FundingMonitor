using FundingMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.Data;

public class AppDbContext : DbContext
{
    public DbSet<Exchange> Exchanges { get; set; }
    public DbSet<TradingPair> TradingPairs { get; set; }
    public DbSet<FundingRate> FundingRates { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        modelBuilder.HasDefaultSchema("public");
    }
}