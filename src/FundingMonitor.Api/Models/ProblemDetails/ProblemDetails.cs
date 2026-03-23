using System.Text.Json.Serialization;

namespace FundingMonitor.Api.Models.ProblemDetails;

/// <summary>
///     Базовый класс для Problem Details (RFC 7807)
/// </summary>
public abstract class ApiProblemDetails
{
    /// <summary>
    ///     URI, идентифицирующий тип проблемы
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "about:blank";

    /// <summary>
    ///     Краткое описание проблемы
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    ///     HTTP код статуса
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; init; }

    /// <summary>
    ///     Подробное описание проблемы
    /// </summary>
    [JsonPropertyName("detail")]
    public string Detail { get; init; } = string.Empty;

    /// <summary>
    ///     URI конкретного экземпляра проблемы
    /// </summary>
    [JsonPropertyName("instance")]
    public string Instance { get; init; } = string.Empty;

    /// <summary>
    ///     Идентификатор запроса
    /// </summary>
    [JsonPropertyName("requestId")]
    public string? RequestId { get; init; }

    /// <summary>
    ///     Временная метка ошибки
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }
}