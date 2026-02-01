using FundingMonitor.Data;
using FundingMonitor.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Настройка DI
var services = new ServiceCollection();

// Настройка БД
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql("Host=localhost;Port=5432;Database=funding_monitor;Username=postgres;Password=postgres"));
    
// Регистрация репозиториев
services.AddScoped<IExchangeRepository, ExchangeRepository>();

var serviceProvider = services.BuildServiceProvider();

// Тестируем
using var scope = serviceProvider.CreateScope();
var exchangeRepo = scope.ServiceProvider.GetRequiredService<IExchangeRepository>();

Console.WriteLine("🚀 Testing Code First Database...");
Console.WriteLine("==================================");

// 1. Получаем все биржи из БД
var exchanges = await exchangeRepo.GetAllAsync();
Console.WriteLine($"\n📊 Found {exchanges.Count} exchanges in database:");

foreach (var exchange in exchanges)
{
    Console.WriteLine($"  - {exchange.Name} ({exchange.ApiBaseUrl})");
}

// 2. Пробуем добавить новую биржу (тест)
Console.WriteLine("\n🧪 Testing repository methods...");

try
{
    var newExchange = new FundingMonitor.Data.Entities.Exchange
    {
        Name = "TestExchange",
        ApiBaseUrl = "https://test.com/api",
        IsActive = true
    };
    
    var addedExchange = await exchangeRepo.AddAsync(newExchange);
    Console.WriteLine($"✅ Added new exchange: {addedExchange.Name} (ID: {addedExchange.Id})");
    
    // Удаляем тестовую биржу
    await exchangeRepo.DeleteAsync(addedExchange.Id);
    Console.WriteLine("✅ Test exchange deactivated");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}

// 3. Проверяем структуру БД через DbContext
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var tableNames = await dbContext.Database
    .SqlQueryRaw<string>("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'")
    .ToListAsync();

Console.WriteLine("\n🗄️ Database tables:");
foreach (var table in tableNames)
{
    Console.WriteLine($"  - {table}");
}

Console.WriteLine("\n🎉 Code First setup completed successfully!");
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();