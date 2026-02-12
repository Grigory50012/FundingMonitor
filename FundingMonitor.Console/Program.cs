using System.Net;
using FundingMonitor.Application.Interfaces.Clients;
using FundingMonitor.Application.Interfaces.Repositories;
using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Application.Services;
using FundingMonitor.Console.Services;
using FundingMonitor.Core.Configuration;
using FundingMonitor.Infrastructure.Data;
using FundingMonitor.Infrastructure.Data.Repositories;
using FundingMonitor.Infrastructure.ExchangeClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace FundingMonitor.Console;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Основные секции конфигурации
                services.Configure<DataCollectionOptions>(
                    context.Configuration.GetSection(DataCollectionOptions.SectionName));

                services.Configure<ConnectionStringsOptions>(
                    context.Configuration.GetSection(ConnectionStringsOptions.SectionName));

                // Конфигурация для каждой биржи
                services.Configure<ExchangeOptions>(
                    ExchangeOptions.BinanceSection,
                    context.Configuration.GetSection(ExchangeOptions.BinanceSection));

                services.Configure<ExchangeOptions>(
                    ExchangeOptions.BybitSection,
                    context.Configuration.GetSection(ExchangeOptions.BybitSection));

                // Настройка БД
                services.AddDbContext<AppDbContext>((sp, options) =>
                {
                    var connectionStrings = sp.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value;
                    options.UseNpgsql(connectionStrings.DefaultConnection);
                });

                // Репозиторий
                services.AddScoped<ICurrentFundingRateRepository, CurrentFundingRateRepository>();

                // Настройка HTTP клиентов с Polly
                ConfigureHttpClients(services, context.Configuration);

                services.AddTransient<IExchangeApiClient, BinanceApiClient>(sp =>
                    sp.GetRequiredService<BinanceApiClient>());
                services.AddTransient<IExchangeApiClient, BybitApiClient>(sp =>
                    sp.GetRequiredService<BybitApiClient>());

                // Services
                services.AddScoped<IDataCollector, DataCollector>();
                services.AddScoped<IArbitrageScanner, ArbitrageScanner>();
                services.AddScoped<IExchangeHealthChecker, ExchangeHealthChecker>();
                services.AddSingleton<ISymbolNormalizer, SymbolNormalizer>();

                // Сервис для фоновой работы
                services.AddHostedService<FundingDataBackgroundService>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();

                var logLevel = context.Configuration.GetValue("Logging:LogLevel:Default", LogLevel.Information);
                logging.SetMinimumLevel(logLevel);

                logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            })
            .Build();

        // Проверяем миграции БД при старте
        await EnsureDatabaseMigratedAsync(host.Services);

        // Отображаем стартовую информацию
        await ShowStartupInfoAsync(host.Services);

        // Запускаем хост
        await host.RunAsync();
    }

    private static void ConfigureHttpClients(IServiceCollection services, IConfiguration configuration)
    {
        var binanceConfig = configuration.GetSection(ExchangeOptions.BinanceSection).Get<ExchangeOptions>();
        var bybitConfig = configuration.GetSection(ExchangeOptions.BybitSection).Get<ExchangeOptions>();

        // Binance
        services.AddHttpClient<BinanceApiClient>((sp, client) =>
            {
                client.BaseAddress = new Uri(binanceConfig.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(binanceConfig.TimeoutSeconds);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            })
            .AddPolicyHandler((sp, _) =>
            {
                var options = sp.GetRequiredService<IOptions<DataCollectionOptions>>().Value;
                return GetRetryPolicy(options.RetryCount);
            })
            .AddPolicyHandler(GetRateLimitPolicy(binanceConfig.RateLimitPerMinute))
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Bybit
        services.AddHttpClient<BybitApiClient>(client =>
            {
                client.BaseAddress = new Uri(bybitConfig.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(bybitConfig.TimeoutSeconds);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            })
            .AddPolicyHandler((sp, _) =>
            {
                var options = sp.GetRequiredService<IOptions<DataCollectionOptions>>().Value;
                return GetRetryPolicy(options.RetryCount);
            })
            .AddPolicyHandler(GetRateLimitPolicy(bybitConfig.RateLimitPerMinute))
            .AddPolicyHandler(GetCircuitBreakerPolicy());
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRateLimitPolicy(int permitsPerSecond)
    {
        return Policy.RateLimitAsync<HttpResponseMessage>(
            numberOfExecutions: permitsPerSecond,
            perTimeSpan: TimeSpan.FromMinutes(1));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (_, timespan, retryAttempt, _) =>
                {
                    System.Console.WriteLine(
                        $"[HTTP] Retry {retryAttempt}/{retryCount} after {timespan.TotalSeconds:F1}s");
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                (_, duration) =>
                    System.Console.WriteLine($"[Circuit] Opened for {duration.TotalSeconds}s"),
                onReset: () => System.Console.WriteLine("[Circuit] Closed"));
    }

    private static async Task EnsureDatabaseMigratedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            await dbContext.Database.MigrateAsync();
            System.Console.WriteLine("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error applying migrations: {ex.Message}");
            throw;
        }
    }

    private static async Task ShowStartupInfoAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var healthChecker = scope.ServiceProvider.GetRequiredService<IExchangeHealthChecker>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        System.Console.Clear();
        System.Console.WriteLine("╔══════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║              FUNDING MONITOR v1.0                    ║");
        System.Console.WriteLine("║          Multi-Exchange Arbitrage Scanner            ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════════╝");
        System.Console.WriteLine();

        try
        {
            System.Console.WriteLine("CHECKING EXCHANGE STATUS...");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var status = await healthChecker.CheckAllExchangesAsync(cts.Token);

            System.Console.WriteLine("\nEXCHANGE STATUS");
            System.Console.WriteLine("───────────────");
            foreach (var (exchange, isAvailable) in status)
            {
                System.Console.WriteLine($"  {exchange,-10} : {(isAvailable ? "Available" : "Unavailable")}");
            }

            // Выводим статус БД
            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var canConnect = await dbContext.Database.CanConnectAsync(cts.Token);
                System.Console.WriteLine($"Database: {(canConnect ? "Connected" : "Not connected")}");
            }
            catch
            {
                System.Console.WriteLine("Database: Connection error");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error showing startup info");
            System.Console.WriteLine($"Error: {ex.Message}");
        }
    }
}