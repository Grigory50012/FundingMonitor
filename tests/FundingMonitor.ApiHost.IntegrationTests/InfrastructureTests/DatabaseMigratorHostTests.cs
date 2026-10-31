using FundingMonitor.DatabaseMigrator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.InfrastructureTests;

public sealed class DatabaseMigratorHostTests
{
    [Fact]
    public void CreateBuilder_RegistersNLogWithoutEventLogProvider()
    {
        using var host = DatabaseMigratorHost.CreateBuilder(["--Logging:Directory=" + Path.Combine(Path.GetTempPath(), "funding-migrator-" + Guid.NewGuid())]).Build();

        using var factory = host.Services.GetRequiredService<NLog.LogFactory>();
        var providers = host.Services.GetServices<ILoggerProvider>().ToArray();

        Assert.Contains(providers, provider => provider.GetType().Name == "NLogLoggerProvider");
        Assert.DoesNotContain(providers, provider => provider.GetType().Name.Contains("EventLog"));
        var target = new NLog.Targets.MemoryTarget("probe") { Layout = "${message}" };
        factory.Configuration!.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, target, "FundingMonitor.Probe");
        factory.ReconfigExistingLoggers();
        host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("FundingMonitor.Probe").LogDebug("debug-enabled");
        Assert.Contains("debug-enabled", target.Logs);
    }
}
