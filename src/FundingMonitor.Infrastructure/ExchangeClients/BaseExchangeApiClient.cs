using System.Text.Json;
using FundingMonitor.Application.Interfaces.Clients;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Extensions;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.ExchangeClients;

public abstract class BaseExchangeApiClient : IExchangeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions = new();
    
    private readonly List<DateTime> _requestTimes = new();
    private readonly int _rateLimit;
    private readonly object _lock = new();
    
    public abstract ExchangeType ExchangeType { get; }
    
    protected BaseExchangeApiClient(
        HttpClient httpClient, 
        ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _rateLimit = ExchangeType.GetRateLimitPerMinute();
        
        ConfigureHttpClient();
        ConfigureJsonOptions();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(ExchangeType.GetApiBaseUrl());
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FundingMonitor/1.0");
    }
    
    private void ConfigureJsonOptions()
    {
        _jsonOptions.PropertyNameCaseInsensitive = true;
        _jsonOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
    }
    
    protected async Task<T> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        await WaitIfRateLimitedAsync(ct);
        
        try
        {
            _logger.LogDebug("[{Exchange}] GET {Endpoint}", ExchangeType, endpoint);
            
            var response = await _httpClient.GetAsync(endpoint, ct);
            RegisterRequest();
            
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
    
    private async Task WaitIfRateLimitedAsync(CancellationToken ct)
    {
        while (IsRateLimited)
        {
            _logger.LogDebug("[{Exchange}] Rate limited, waiting...", ExchangeType);
            await Task.Delay(1000, ct);
        }
    }
    
    public bool IsRateLimited
    {
        get
        {
            lock (_lock)
            {
                var minuteAgo = DateTime.UtcNow.AddMinutes(-1);
                _requestTimes.RemoveAll(t => t < minuteAgo);
                return _requestTimes.Count >= _rateLimit;
            }
        }
    }
    
    private void RegisterRequest()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            _requestTimes.Add(now);
            
            if (_requestTimes.Count > _rateLimit * 2) // Храним максимум в 2 раза больше лимита
            {
                _requestTimes.RemoveAll(t => t < now.AddMinutes(-2));
            }
        }
    }
    
    // Абстрактные методы
    public abstract Task<List<NormalizedFundingRate>> GetAllFundingRatesAsync(CancellationToken  cancellationToken);
    
    public virtual async Task<bool> IsAvailableAsync(CancellationToken  cancellationToken)
    {
        try
        {
            await _httpClient.GetAsync("/", cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    protected static decimal SafeParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;
        
        return decimal.TryParse(value, System.Globalization.NumberStyles.Any, 
            System.Globalization.CultureInfo.InvariantCulture, out var result) 
            ? result : 0m;
    }
}