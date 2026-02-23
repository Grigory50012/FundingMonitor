using FundingMonitor.Application.Extensions;
using FundingMonitor.Console.Services;
using FundingMonitor.Core.Extensions;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Services;
using FundingMonitor.Infrastructure.Data;
using FundingMonitor.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                // Регистрация всех сервисов через extension методы
                services.AddCoreServices(context.Configuration);
                services.AddInfrastructureServices(context.Configuration);
                services.AddApplicationServices();

                // Background services
                services.AddHostedService<CurrentDataBackgroundService>();
                services.AddHostedService<RabbitMqInitializer>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddDebug();

                var logLevel = context.Configuration.GetValue("Logging:LogLevel:Default", LogLevel.Information);
                logging.SetMinimumLevel(logLevel);

                logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
                logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
            })
            .Build();

        // Проверяем миграции БД при старте
        await EnsureDatabaseMigratedAsync(host.Services);

        // Отображаем стартовую информацию
        await ShowStartupInfoAsync(host.Services);

        // Запускаем хост
        await host.RunAsync();
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

            // Проверка rate limiter
            System.Console.WriteLine("\nTESTING RATE LIMITER...");
            var clients = scope.ServiceProvider.GetServices<IExchangeApiClient>();
            foreach (var client in clients)
                System.Console.WriteLine($"{client.ExchangeType} : {client.GetType().Name}");

            // Проверка подключения к БД
            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var canConnect = await dbContext.Database.CanConnectAsync(cts.Token);
                System.Console.WriteLine($"\nDATABASE: {(canConnect ? "✓ Connected" : "✗ Not connected")}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nDATABASE: ✗ Error - {ex.Message}");
            }

            System.Console.WriteLine("\n✓ System ready. Press Ctrl+C to stop.");
            System.Console.WriteLine();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }
    }
}