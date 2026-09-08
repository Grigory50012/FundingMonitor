using FundingMonitor.CurrentRates.Infrastructure.Persistence;
using FundingMonitor.HistoricalRates.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.DatabaseMigrator;

public sealed class ModuleDatabaseMigrator(
    CurrentRatesDbContext currentRates,
    HistoricalRatesDbContext historicalRates)
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await currentRates.Database.MigrateAsync(cancellationToken);
        await historicalRates.Database.MigrateAsync(cancellationToken);
    }
}
