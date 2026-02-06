using System.Net;
using FundingMonitor.Core.Interfaces;
using FundingMonitor.Core.Services;
using FundingMonitor.Core.Services.Exchanges;
using FundingMonitor.Data;
using FundingMonitor.Data.Repositories;
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
            .ConfigureServices((context, services) =>
            {
                // Конфигурация
                var configuration = context.Configuration;
                
                // Настройка БД
                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection") ??
                                      "Host=localhost;Port=5432;Database=funding_monitor;Username=postgres;Password=postgres"));
                
                // Репозиторий
                services.AddScoped<IFundingRateRepository, FundingRateRepository>();
                
                // Вспомогательные сервисы
                services.AddSingleton<SymbolNormalizer>();
                
                // Базовый HttpClient без специфичных настроек
                services.AddHttpClient();
                
                // HttpClient для каждой биржи с индивидуальными настройками и Polly политиками
                services.AddHttpClient<BinanceApiClient>(client =>
                    {
                        client.BaseAddress = new Uri("https://fapi.binance.com");
                        client.Timeout = TimeSpan.FromSeconds(30);
                    })
                    .AddPolicyHandler(GetRetryPolicy())
                    .AddPolicyHandler(GetCircuitBreakerPolicy());
                
                services.AddHttpClient<BybitApiClient>(client =>
                    {
                        client.BaseAddress = new Uri("https://api.bybit.com");
                        client.Timeout = TimeSpan.FromSeconds(30);
                    })
                    .AddPolicyHandler(GetRetryPolicy())
                    .AddPolicyHandler(GetCircuitBreakerPolicy());
                
                // Именованный HttpClient для общего использования
                services.AddHttpClient("ExchangeClient", client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                    })
                    .AddPolicyHandler(GetRetryPolicy());
                
                // API клиенты
                services.AddTransient<IExchangeApiClient, BinanceApiClient>();
                services.AddTransient<IExchangeApiClient, BybitApiClient>();
                // services.AddTransient<IExchangeApiClient, OkxApiClient>();
                
                // Основной сервис
                services.AddScoped<IFundingDataService, FundingDataService>();
                
                // Сервис для фоновой работы
                services.AddHostedService<FundingDataBackgroundService>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            })
            .Build();
        
        // Отображаем стартовую информацию
        await ShowStartupInfoAsync(host.Services);
        
        // Запускаем хост
        await host.RunAsync();
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
            System.Console.WriteLine("Checking exchanges availability...");
            var status = await dataService.CheckExchangesStatusAsync();
            
            System.Console.WriteLine("\nEXCHANGE STATUS");
            System.Console.WriteLine("───────────────");
            foreach (var (exchange, isAvailable) in status)
            {
                System.Console.WriteLine($"  {exchange,-10} : {(isAvailable ? "✅ Available" : "❌ Unavailable")}");
            }
            
            System.Console.WriteLine($"\nBackground service started at: {DateTime.Now:HH:mm:ss}");
            System.Console.WriteLine("Data will be collected every minute");
            System.Console.WriteLine("\nPress Ctrl+C to stop...");
            System.Console.WriteLine();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error showing startup info");
            System.Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    // Политика повторных попыток
    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    System.Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds:F1}s due to: {outcome.Result?.StatusCode}");
                });
    }
    
    // Политика Circuit Breaker
    static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, breakDelay) =>
                {
                    System.Console.WriteLine($"Circuit breaker opened for {breakDelay.TotalSeconds}s due to: {outcome.Exception?.Message}");
                },
                onReset: () =>
                {
                    System.Console.WriteLine("Circuit breaker reset");
                });
    }
}