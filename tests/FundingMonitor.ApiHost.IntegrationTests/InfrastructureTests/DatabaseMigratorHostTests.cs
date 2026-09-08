using FundingMonitor.DatabaseMigrator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.InfrastructureTests;

public sealed class DatabaseMigratorHostTests
{
    [Fact]
    public void CreateBuilder_RegistersConsoleLoggingWithoutEventLogProvider()
    {
        using var host = DatabaseMigratorHost.CreateBuilder([]).Build();

        var providers = host.Services.GetServices<ILoggerProvider>().ToArray();

        Assert.Contains(providers, provider => provider.GetType().Name == "ConsoleLoggerProvider");
        Assert.DoesNotContain(providers, provider => provider.GetType().Name.Contains("EventLog"));
    }
}
