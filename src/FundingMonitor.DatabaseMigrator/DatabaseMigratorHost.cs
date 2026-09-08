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
        builder.Logging.AddConsole();

        return builder;
    }
}
