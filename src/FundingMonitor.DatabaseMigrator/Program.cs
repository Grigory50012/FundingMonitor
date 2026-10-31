using FundingMonitor.CurrentRates.Infrastructure;
using FundingMonitor.HistoricalRates.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.DatabaseMigrator;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = DatabaseMigratorHost.CreateBuilder(args);
        using var logFactory = (NLog.LogFactory)builder.Services.Single(d => d.ServiceType == typeof(NLog.LogFactory)).ImplementationInstance!;
        using var bootstrapLoggerFactory = LoggerFactory.Create(logging => FundingMonitor.Hosting.LocalLogging.Add(logging, logFactory));
        var logger = bootstrapLoggerFactory.CreateLogger("DatabaseMigrator");
        try
        {

            builder.Services.AddCurrentRatesPersistence(builder.Configuration);
            builder.Services.AddHistoricalRatesPersistence(builder.Configuration);
            builder.Services.AddScoped<ModuleDatabaseMigrator>();
            using var host = builder.Build();
            await using var scope = host.Services.CreateAsyncScope();
            logger.LogInformation("Applying module database migrations");
            await scope.ServiceProvider.GetRequiredService<ModuleDatabaseMigrator>().MigrateAsync();
            logger.LogInformation("All module database migrations applied successfully.");
            return 0;
        }
        catch (HostAbortedException)
        {
            throw; // EF tooling stops host discovery before executing migrations.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Module database migration failed.");
            return 1;
        }
        finally { logFactory.Flush(TimeSpan.FromSeconds(5)); }
    }
}
