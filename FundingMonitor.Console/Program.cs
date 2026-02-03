using FundingMonitor.Core.Interfaces;
using FundingMonitor.Core.Services;
using FundingMonitor.Data;
using FundingMonitor.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

// Настройка логирования
services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Настройка БД
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql("Host=localhost;Port=5432;Database=funding_monitor;Username=postgres;Password=postgres"));
    
// Репозиторий
services.AddScoped<IFundingRateRepository, FundingRateRepository>();

// Настройка HttpClient
services.AddHttpClient();

// Вспомогательные сервисы
services.AddSingleton<SymbolNormalizer>();

// HttpClient для каждой биржи
services.AddHttpClient<BinanceApiClient>();
services.AddHttpClient<BybitApiClient>();

// API клиенты
services.AddTransient<IExchangeApiClient, BinanceApiClient>();
services.AddTransient<IExchangeApiClient, BybitApiClient>();

// Основной сервис
services.AddScoped<IFundingDataService, FundingDataService>();

var serviceProvider = services.BuildServiceProvider();

// Создаем scope для scoped сервисов
using var scope = serviceProvider.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var dataService = scope.ServiceProvider.GetRequiredService<IFundingDataService>();
var repository = scope.ServiceProvider.GetRequiredService<IFundingRateRepository>();

Console.Clear();
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║              FUNDING MONITOR v1.0                    ║");
Console.WriteLine("║          Multi-Exchange Arbitrage Scanner            ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();

try
{
    // 1. Проверяем доступность бирж
    logger.LogInformation("Checking exchanges availability...");
    var status = await dataService.CheckExchangesStatusAsync();
    
    Console.WriteLine("EXCHANGE STATUS");
    Console.WriteLine("───────────────");
    foreach (var (exchange, isAvailable) in status)
    {
        Console.WriteLine($"  {exchange,-10} : {(isAvailable ? "Available" : "Unavailable")}");
    }
    Console.WriteLine();
    
    // 2. Собираем данные
    Console.WriteLine("FUNDING rates");
    Console.WriteLine("─────────────");
    var allRates = await dataService.CollectAllRatesAsync();
    
    Console.WriteLine($"Collected {allRates.Count} funding rates");
    Console.WriteLine();
    
    // 3. Сохраняем в БД
    Console.WriteLine("Save to database");
    Console.WriteLine("─────────────────");
    if (allRates.Any())
    {
        await repository.SaveRatesAsync(allRates);
        Console.WriteLine("Saved to database");
        Console.WriteLine();
    }
    
    // 4. Ищем арбитражные возможности
    logger.LogInformation("Scanning for arbitrage opportunities...");
    var opportunities = dataService.FindArbitrageOpportunitiesAsync(allRates);
    
    if (opportunities.Any())
    {
        Console.WriteLine("💰 ARBITRAGE OPPORTUNITIES");
        Console.WriteLine("──────────────────────────");
        
        foreach (var opp in opportunities.Take(10)) // Показываем топ-10
        {
            Console.WriteLine();
            Console.WriteLine($"  {opp.Symbol}");
            Console.WriteLine($"    Difference: {opp.MaxDifference:P4}");
            Console.WriteLine($"    Annual yield: {opp.AnnualYieldPercent:F2}%");
            Console.WriteLine($"    Action: {opp.Action}");
            
            foreach (var rate in opp.Rates.OrderBy(r => r.Exchange))
            {
                Console.WriteLine($"    {rate.Exchange,-10}: {rate.FundingRate:P6} ({rate.NextFundingTime:HH:mm})");
            }
        }
        
        if (opportunities.Count > 10)
        {
            Console.WriteLine($"\n  ... and {opportunities.Count - 10} more opportunities");
        }
    }
    else
    {
        Console.WriteLine("🤷 No significant arbitrage opportunities found");
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Application error");
    Console.WriteLine($"\nError: {ex.Message}");
    
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner: {ex.InnerException.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("Scan completed!");
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();