using Microsoft.Extensions.Logging;
using FundingMonitor.CurrentRates.Application.Collection;
using FundingMonitor.CurrentRates.Application.Persistence;
using FundingMonitor.CurrentRates.Infrastructure.Persistence;
using FundingMonitor.CurrentRates.Infrastructure.Outbox;
using FundingMonitor.Messaging;
using FundingMonitor.Messaging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Xunit;

namespace FundingMonitor.CurrentRates.IntegrationTests.Infrastructure;

public sealed class CurrentRatesPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("current_rates_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = await CreateDbContextAsync();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task ResetAsync()
    {
        await using var context = await CreateDbContextAsync();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE current_rates.\"CurrentFundingRates\", current_rates.\"OutboxMessages\" RESTART IDENTITY CASCADE");
    }

    public Task<CurrentRatesDbContext> CreateDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<CurrentRatesDbContext>()
            .UseNpgsql(ConnectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "current_rates"))
            .Options;
        return Task.FromResult(new CurrentRatesDbContext(options));
    }

    public ValueTask<AsyncServiceScope> CreateScopeAsync(
        SaveChangesInterceptor? interceptor = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Options.Create(new CurrentDataCollectionOptions
        {
            DeactivateMissingAfterMinutes = 5
        }));
        services.AddDbContext<CurrentRatesDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString);
            if (interceptor is not null)
                options.AddInterceptors(interceptor);
        });
        services.AddScoped<CurrentRatesRepository>();
        services.AddScoped<ICurrentRatesRepository>(provider =>
            provider.GetRequiredService<CurrentRatesRepository>());
        services.AddScoped<ICurrentRatesUnitOfWork>(provider =>
            provider.GetRequiredService<CurrentRatesRepository>());

        return ValueTask.FromResult(services.BuildServiceProvider().CreateAsyncScope());
    }

    internal async Task SeedOutboxAsync(OutboxMessage message)
    {
        await using var context = await CreateDbContextAsync();
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    internal ValueTask<OutboxPublisherTestScope> CreatePublisherScopeAsync(
        IMessagePublisher publisher,
        DateTimeOffset now, ILogger<OutboxPublisher>? logger = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (logger is not null) services.AddSingleton(logger);
        services.AddSingleton<IMessagePublisher>(publisher);
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(now));
        services.Configure<OutboxOptions>(options =>
        {
            options.BatchSize = 10;
        });
        services.AddDbContextFactory<CurrentRatesDbContext>(options =>
            options.UseNpgsql(ConnectionString));
        services.AddSingleton<OutboxMetrics>();
        services.AddSingleton<OutboxPublisher>();
        var provider = services.BuildServiceProvider();
        return ValueTask.FromResult(new OutboxPublisherTestScope(
            provider,
            provider.GetRequiredService<OutboxPublisher>()));
    }
}

internal sealed class OutboxPublisherTestScope(
    ServiceProvider provider,
    OutboxPublisher publisher) : IAsyncDisposable
{
    public OutboxPublisher Publisher { get; } = publisher;

    public ValueTask DisposeAsync() => provider.DisposeAsync();
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
