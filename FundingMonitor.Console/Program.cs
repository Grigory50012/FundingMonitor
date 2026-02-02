using FundingMonitor.Core.Interfaces;
using FundingMonitor.Core.Services;
using FundingMonitor.Data;
using FundingMonitor.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Настройка DI
var services = new ServiceCollection();

// Настройка логирования
services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Information);
});

// Настройка БД
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql("Host=localhost;Port=5432;Database=funding_monitor;Username=postgres;Password=postgres"));
    
// Регистрация репозиториев
services.AddScoped<IExchangeRepository, ExchangeRepository>();

// Настройка HttpClient
services.AddHttpClient();

// Регистрация API клиентов
// Настройка HttpClient для Binance
services.AddHttpClient<BinanceApiClient>(client =>
{
    client.BaseAddress = new Uri("https://fapi.binance.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Настройка HttpClient для Bybit
services.AddHttpClient<BybitApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.bybit.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Регистрируем конкретные реализации
services.AddTransient<BinanceApiClient>();
services.AddTransient<BybitApiClient>();

// Регистрируем фабрику для IExchangeApiClient
services.AddTransient<Func<string, IExchangeApiClient>>(serviceProvider => exchangeName =>
{
    return exchangeName.ToLower() switch
    {
        "binance" => serviceProvider.GetRequiredService<BinanceApiClient>(),
        "bybit" => serviceProvider.GetRequiredService<BybitApiClient>(),
        _ => throw new ArgumentException($"Unknown exchange: {exchangeName}")
    };
});

// Регистрация сервисов
services.AddScoped<IFundingDataService, FundingDataService>();

var serviceProvider = services.BuildServiceProvider();

// Тестируем
using var scope = serviceProvider.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var dataService = scope.ServiceProvider.GetRequiredService<IFundingDataService>();

Console.WriteLine("🚀 Funding Monitor - Data Collection Test");
Console.WriteLine("==========================================");
Console.WriteLine();

try
{
    // 1. Тестируем подключение к биржам
    logger.LogInformation("Testing exchange connections...");
    
    var binanceClient = scope.ServiceProvider.GetRequiredService<BinanceApiClient>();
    var bybitClient = scope.ServiceProvider.GetRequiredService<BybitApiClient>();
    
    // 2. Получаем пары с бирж
    logger.LogInformation("Fetching pairs from Binance...");
    var binancePairs = await binanceClient.GetAvailablePairsAsync();
    Console.WriteLine($"✅ Binance: {binancePairs.Count} perpetual pairs");
    
    logger.LogInformation("Fetching pairs from Bybit...");
    var bybitPairs = await bybitClient.GetAvailablePairsAsync();
    Console.WriteLine($"✅ Bybit: {bybitPairs.Count} perpetual pairs");
    
    // 3. Обновляем базу данных
    logger.LogInformation("Updating database...");
    await dataService.UpdateDatabaseFromExchangesAsync();
    Console.WriteLine("✅ Database updated");
    
    // 4. Сравниваем ставки финансирования
    logger.LogInformation("Comparing funding rates...");
    var comparisons = await dataService.CompareFundingRatesAsync();
    
    Console.WriteLine();
    Console.WriteLine("📊 FUNDING RATE COMPARISONS");
    Console.WriteLine("=============================");
    
    if (comparisons.Any())
    {
        foreach (var comparison in comparisons.Take(5)) // Показываем топ-5
        {
            Console.WriteLine();
            Console.WriteLine($"💰 {comparison.Symbol}");
            Console.WriteLine($"   Binance:  {comparison.BinanceRate.Rate:P6}");
            Console.WriteLine($"   Bybit:    {comparison.BybitRate.Rate:P6}");
            Console.WriteLine($"   Difference: {comparison.Difference:P6} ({comparison.PotentialProfit:F2}% annual)");
            Console.WriteLine($"   Action: {comparison.SuggestedAction}");
        }
        
        if (comparisons.Count > 5)
        {
            Console.WriteLine($"\n... and {comparisons.Count - 5} more opportunities");
        }
    }
    else
    {
        Console.WriteLine("No significant arbitrage opportunities found.");
    }
    
    // 5. Показываем статистику из БД
    Console.WriteLine();
    Console.WriteLine("🗄️ DATABASE STATISTICS");
    Console.WriteLine("======================");
    
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    var exchangeCount = await dbContext.Exchanges.CountAsync();
    var pairCount = await dbContext.TradingPairs.CountAsync();
    var rateCount = await dbContext.FundingRates.CountAsync();
    
    Console.WriteLine($"Exchanges: {exchangeCount}");
    Console.WriteLine($"Trading pairs: {pairCount}");
    Console.WriteLine($"Funding rates collected: {rateCount}");
    
    // Последние 5 ставок
    var latestRates = await dbContext.FundingRates
        .Include(f => f.Exchange)
        .Include(f => f.Pair)
        .OrderByDescending(f => f.CreatedAt)
        .Take(5)
        .Select(f => new
        {
            f.Exchange.Name,
            f.Pair.Symbol,
            f.Rate,
            f.FundingTime
        })
        .ToListAsync();
    
    Console.WriteLine("\n📈 Latest funding rates:");
    foreach (var rate in latestRates)
    {
        Console.WriteLine($"   {rate.Name} {rate.Symbol}: {rate.Rate:P6} (next: {rate.FundingTime:HH:mm})");
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Test failed");
    Console.WriteLine($"❌ Error: {ex.Message}");
    
    if (ex.InnerException != null)
    {
        Console.WriteLine($"   Inner: {ex.InnerException.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("🎉 Test completed!");
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();