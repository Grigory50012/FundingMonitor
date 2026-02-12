using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using FundingMonitor.Application.Interfaces.Clients;
using FundingMonitor.Core.Entities;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.ExchangeClients;

public abstract class BaseExchangeApiClient : IExchangeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new();
    private readonly ILogger _logger;

    protected BaseExchangeApiClient(
        HttpClient httpClient,
        ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        ConfigureJsonOptions();
    }

    public abstract ExchangeType ExchangeType { get; }

    // Абстрактные методы
    public abstract Task<List<CurrentFundingRate>> GetAllFundingRatesAsync(CancellationToken cancellationToken);

    public abstract Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

    private void ConfigureJsonOptions()
    {
        _jsonOptions.PropertyNameCaseInsensitive = true;
        _jsonOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
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

    protected static decimal SafeParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;

        return decimal.TryParse(value, NumberStyles.Any,
            CultureInfo.InvariantCulture, out var result)
            ? result
            : 0m;
    }
}