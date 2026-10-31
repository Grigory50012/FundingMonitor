using System.Text.Json.Serialization;
using FundingMonitor.ApiHost.Shared.Middleware;
using FundingMonitor.Arbitrage.Presentation;
using FundingMonitor.CurrentRates.Presentation;
using FundingMonitor.ExchangeIntegration.Presentation;
using FundingMonitor.HistoricalRates.Presentation;
using FundingMonitor.Hosting;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

using var logFactory = LocalLogging.Create(builder.Configuration, "api-host", "nlog.config");
LocalLogging.Add(builder.Logging, logFactory);
try
{
    builder.Services.AddControllers().AddJsonOptions(options =>
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict);
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
    builder.Services.AddOpenApi();
    builder.Services.AddProblemDetails();
    builder.Services.AddAuthorization();
    builder.Services.AddExchangeIntegrationModule(builder.Configuration);
    builder.Services.AddCurrentRatesModule(builder.Configuration);
    builder.Services.AddHistoricalRatesModule(builder.Configuration);
    builder.Services.AddArbitrageModule(builder.Configuration);
    builder.Services.AddCors(options => options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
    builder.Services.AddResponseCaching();
    builder.Services.AddMemoryCache();
    builder.Services.AddHealthChecks();

    var app = builder.Build();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseResponseCaching();
    app.UseAuthorization();
    app.MapControllers();
    app.MapExchangeIntegrationEndpoints();
    app.MapCurrentRatesEndpoints();
    app.MapHistoricalRatesEndpoints();
    app.MapArbitrageEndpoints();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Microsoft.Extensions.Hosting.HostAbortedException)
{
    throw;
}
catch (Exception exception)
{
    logFactory.GetLogger("FundingMonitor.Hosting").Fatal(exception, "Application terminated unexpectedly");
    throw;
}
finally
{
    logFactory.Flush(TimeSpan.FromSeconds(5));
}

public partial class Program;
