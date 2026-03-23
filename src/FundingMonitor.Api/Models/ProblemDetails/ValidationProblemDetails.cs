using System.Text.Json.Serialization;

namespace FundingMonitor.Api.Models.ProblemDetails;

/// <summary>
///     Problem Details для ошибок валидации (400 Bad Request)
/// </summary>
public class ApiValidationProblemDetails : ApiProblemDetails
{
    /// <summary>
    ///     Детали ошибок валидации по полям
    /// </summary>
    [JsonPropertyName("errors")]
    public Dictionary<string, string[]> Errors { get; init; } = new();
}