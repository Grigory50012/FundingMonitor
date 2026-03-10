using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FundingMonitor.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<FundingMonitorDbContext>
{
    public FundingMonitorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FundingMonitorDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=funding_monitor;Username=postgres;Password=postgres");

        return new FundingMonitorDbContext(optionsBuilder.Options);
    }
}