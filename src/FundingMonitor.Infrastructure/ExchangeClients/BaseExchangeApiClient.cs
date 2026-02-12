using System.Text.Json;
using FundingMonitor.Application.Interfaces.Clients;
using FundingMonitor.Core.Entities;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.ExchangeClients;

public abstract class BaseExchangeApiClient : IExchangeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions = new();
    
    public abstract ExchangeType ExchangeType { get; }
    
    protected BaseExchangeApiClient(
        HttpClient httpClient, 
        ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        ConfigureJsonOptions();
    }

    private void ConfigureJsonOptions()
    {
        _jsonOptions.PropertyNameCaseInsensitive = true;
        _jsonOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
    }
    
    protected async Task<T> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            
            return result ?? throw new InvalidOperationException("Deserialization failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Exchange}] Request failed", ExchangeType);
            throw;
        }
    }
    
    // Абстрактные методы
    public abstract Task<List<NormalizedFundingRate>> GetAllFundingRatesAsync(CancellationToken  cancellationToken);

    public abstract Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
    
    protected static decimal SafeParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;
        
        return decimal.TryParse(value, System.Globalization.NumberStyles.Any, 
            System.Globalization.CultureInfo.InvariantCulture, out var result) 
            ? result : 0m;
    }
}