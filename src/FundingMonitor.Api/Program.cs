using System.Reflection;
using FundingMonitor.Api.Middleware;
using FundingMonitor.Api.OpenApi;
using FundingMonitor.Application.Extensions;
using FundingMonitor.Core.Extensions;
using FundingMonitor.Infrastructure.Data;
using FundingMonitor.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using NLog.Web;
using Swashbuckle.AspNetCore.Swagger;

var exportArgumentIndex = Array.IndexOf(args, "--export-openapi");
var exportPath = exportArgumentIndex >= 0 && exportArgumentIndex + 1 < args.Length
    ? args[exportArgumentIndex + 1]
    : null;
var applicationArgs = exportArgumentIndex >= 0
    ? args.Where((_, index) => index != exportArgumentIndex && index != exportArgumentIndex + 1).ToArray()
    : args;

var builder = WebApplication.CreateBuilder(applicationArgs);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SupportNonNullableReferenceTypes();
    c.NonNullableReferenceTypesAsRequired();
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Funding Monitor API",
        Version = "v1",
        Description = "API для мониторинга ставок финансирования на криптовалютных биржах"
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddCoreServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

var app = builder.Build();

if (exportPath is not null)
{
    var swaggerProvider = app.Services.GetRequiredService<ISwaggerProvider>();
    var document = swaggerProvider.GetSwagger("v1");
    await OpenApiDocumentWriter.WriteAsync(document, exportPath);
    return;
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Funding Monitor API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseResponseCaching();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FundingMonitorDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
