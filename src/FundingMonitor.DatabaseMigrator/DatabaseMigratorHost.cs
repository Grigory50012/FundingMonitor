using Microsoft.Extensions.DependencyInjection;
using FundingMonitor.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.DatabaseMigrator;

public static class DatabaseMigratorHost
{
    public static HostApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Logging.ClearProviders();
        var factory = LocalLogging.Create(builder.Configuration, "database-migrator", "nlog.migrator.config");
        LocalLogging.Add(builder.Logging, factory);
        builder.Services.AddSingleton(factory);

        return builder;
    }
}
