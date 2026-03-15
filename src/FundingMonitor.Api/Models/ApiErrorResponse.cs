namespace FundingMonitor.Api.Models;

public class ApiErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? RequestId { get; set; }
}