using System.Net;
using FundingMonitor.Application.Interfaces.Clients;
using FundingMonitor.Application.Interfaces.Repositories;
using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Application.Services;
using FundingMonitor.Application.Utilities;
using FundingMonitor.Console.Services;
using FundingMonitor.Infrastructure.Data;
using FundingMonitor.Infrastructure.Data.Repositories;
using FundingMonitor.Infrastructure.ExchangeClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                // Конфигурация
                var configuration = context.Configuration;
                
                // Настройка БД
                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
                
                // Репозиторий
                services.AddScoped<IFundingRateRepository, FundingRateRepository>();
                
                // Вспомогательные сервисы
                services.AddSingleton<SymbolNormalizer>();
                
                // Настройка HTTP клиентов с Polly
                ConfigureHttpClients(services, configuration);
                
                // API клиенты
                services.AddTransient<IExchangeApiClient, BinanceApiClient>();
                services.AddTransient<IExchangeApiClient, BybitApiClient>();
                // services.AddTransient<IExchangeApiClient, OkxApiClient>();
                
                // Services
                services.AddScoped<IDataCollector, DataCollector>();
                services.AddScoped<IArbitrageScanner, ArbitrageScanner>();
                services.AddScoped<IExchangeHealthChecker, ExchangeHealthChecker>();
                services.AddScoped<IFundingDataService, FundingDataOrchestrator>();
                
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
        var retryCount = configuration.GetValue("DataCollection:RetryCount", 3);
        
        // Политика повторных попыток
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (_, timespan, retryCountArg, _) =>
                {
                    System.Console.WriteLine($"[HTTP] Retry {retryCountArg}/{retryCountArg} after {timespan.TotalSeconds:F1}s");
                });
        
        // Circuit Breaker
        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
        
        // Binance клиент
        var binanceConfig = configuration.GetSection("Exchanges:Binance");
        services.AddHttpClient<BinanceApiClient>(client =>
            {
                client.BaseAddress = new Uri(binanceConfig["BaseUrl"] ?? "https://fapi.binance.com");
                client.Timeout = TimeSpan.FromSeconds(binanceConfig.GetValue("TimeoutSeconds", 30));
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                
                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            })
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(circuitBreakerPolicy);
        
        // Bybit клиент
        var bybitConfig = configuration.GetSection("Exchanges:Bybit");
        services.AddHttpClient<BybitApiClient>(client =>
            {
                client.BaseAddress = new Uri(bybitConfig["BaseUrl"] ?? "https://api.bybit.com");
                client.Timeout = TimeSpan.FromSeconds(bybitConfig.GetValue("TimeoutSeconds", 30));
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                
                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            })
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(circuitBreakerPolicy);
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
        var dataService = scope.ServiceProvider.GetRequiredService<IFundingDataService>();
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
            var status = await dataService.CheckExchangesStatusAsync();
            
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
                var canConnect = await dbContext.Database.CanConnectAsync();
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
