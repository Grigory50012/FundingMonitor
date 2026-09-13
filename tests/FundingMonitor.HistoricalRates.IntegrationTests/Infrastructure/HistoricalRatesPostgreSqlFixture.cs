using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.HistoricalRates.Application;
using FundingMonitor.HistoricalRates.Application.Persistence;
using FundingMonitor.HistoricalRates.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Xunit;

namespace FundingMonitor.HistoricalRates.IntegrationTests.Infrastructure;

public sealed class HistoricalRatesPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("historical_rates_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = await CreateDbContextAsync();
        await context.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    public async Task ResetAsync()
    {
        await using var context = await CreateDbContextAsync();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE historical_rates.\"HistoricalFundingRates\" CASCADE",
            TestContext.Current.CancellationToken);
    }

    public Task<HistoricalRatesDbContext> CreateDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<HistoricalRatesDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "historical_rates")).Options;
        return Task.FromResult(new HistoricalRatesDbContext(options));
    }

    public ValueTask<AsyncServiceScope> CreateScopeAsync(
        IExchangeMarketDataModule exchangeModule)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(exchangeModule);
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(
            new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero)));
        services.AddSingleton(Options.Create(new HistoricalDataCollectionOptions()));
        services.AddDbContext<HistoricalRatesDbContext>(options =>
        {
            options.UseNpgsql(_container.GetConnectionString());
        });
        services.AddScoped<HistoricalRatesRepository>();
        services.AddScoped<IHistoricalRatesRepository>(provider => provider.GetRequiredService<HistoricalRatesRepository>());
        services.AddScoped<IHistoricalRatesUnitOfWork>(provider => provider.GetRequiredService<HistoricalRatesRepository>());
        services.AddScoped<IHistoricalEventHandler, HistoricalEventHandler>();
        return ValueTask.FromResult(services.BuildServiceProvider().CreateAsyncScope());
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

[CollectionDefinition("HistoricalRatesPostgreSql")]
public sealed class HistoricalRatesPostgreSqlCollection :
    ICollectionFixture<HistoricalRatesPostgreSqlFixture>;
