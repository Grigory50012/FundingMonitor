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

                // Настройка БД
                services.AddDbContext<AppDbContext>((sp, options) =>
                {
                    var connectionStrings = sp.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value;
                    options.UseNpgsql(connectionStrings.DefaultConnection);
                });

                // Репозиторий
                services.AddScoped<ICurrentFundingRateRepository, CurrentFundingRateRepository>();

                // Настройка HTTP клиентов с Polly
                ConfigureExchangeClients(services, context.Configuration);

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

    private static void ConfigureExchangeClients(IServiceCollection services, IConfiguration configuration)
    {
        // Регистрируем конфигурации для бирж
        services.Configure<ExchangeOptions>(
            ExchangeOptions.BinanceSection,
            configuration.GetSection(ExchangeOptions.BinanceSection));

        services.Configure<ExchangeOptions>(
            ExchangeOptions.BybitSection,
            configuration.GetSection(ExchangeOptions.BybitSection));

        // Регистрируем клиентов
        services.AddSingleton<BinanceApiClient>();
        services.AddSingleton<BybitApiClient>();

        // Регистрируем через интерфейс
        services.AddSingleton<IExchangeApiClient, BinanceApiClient>(sp =>
            sp.GetRequiredService<BinanceApiClient>());
        services.AddSingleton<IExchangeApiClient, BybitApiClient>(sp =>
            sp.GetRequiredService<BybitApiClient>());
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