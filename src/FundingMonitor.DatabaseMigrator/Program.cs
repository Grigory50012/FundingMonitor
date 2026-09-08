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
        using var bootstrapLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
        var logger = bootstrapLoggerFactory.CreateLogger("DatabaseMigrator");
        try
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                Args = args,
                ContentRootPath = AppContext.BaseDirectory
            });
            builder.Services.AddCurrentRatesPersistence(builder.Configuration);
            builder.Services.AddHistoricalRatesPersistence(builder.Configuration);
            builder.Services.AddScoped<ModuleDatabaseMigrator>();
            using var host = builder.Build();
            await using var scope = host.Services.CreateAsyncScope();
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
    }
}
