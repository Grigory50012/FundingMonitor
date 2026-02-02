using System.Text.Json;
using FundingMonitor.Core.Interfaces;
using FundingMonitor.Core.Models;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Core.Services;

public abstract class BaseApiClient : IExchangeApiClient
{
    protected readonly HttpClient _httpClient;
    protected readonly JsonSerializerOptions _jsonOptions;
    protected readonly ILogger _logger;
    
    public abstract string ExchangeName { get; }
    
    protected BaseApiClient(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };
        
        // Настраиваем HttpClient
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "FundingMonitor/1.0");
    }
    
    public abstract Task<List<TradingPairInfo>> GetAvailablePairsAsync();
    public abstract Task<FundingRateInfo> GetCurrentFundingRateAsync(string symbol);
    public abstract Task<List<FundingRateInfo>> GetFundingRateHistoryAsync(string symbol, int limit = 100);
    public abstract Task<decimal?> GetPredictedFundingRateAsync(string symbol);

    protected async Task<T> GetAsync<T>(string endpoint)
    {
        try
        {
            _logger.LogDebug("Requesting {Endpoint} from {Exchange}", endpoint, ExchangeName);
            
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            
            return JsonSerializer.Deserialize<T>(json, _jsonOptions) 
                   ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error requesting {Endpoint} from {Exchange}: {StatusCode}", 
                endpoint, ExchangeName, ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting {Endpoint} from {Exchange}", endpoint, ExchangeName);
            throw;
        }
    }
    
    protected async Task<T> GetAsync<T>(string endpoint, Dictionary<string, string> parameters)
    {
        var queryString = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var fullUrl = $"{endpoint}?{queryString}";
        return await GetAsync<T>(fullUrl);
    }
}