using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FundingMonitor.Api.Models.ProblemDetails;
using FundingMonitor.Core.Exceptions;

namespace FundingMonitor.Api.Middleware;

/// <summary>
///     Middleware для глобальной обработки исключений в HTTP pipeline
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = Activity.Current?.Id ?? context.TraceIdentifier;
        var path = context.Request.Path;
        var method = context.Request.Method;

        // Логируем исключение с деталями
        _logger.LogError(
            exception,
            "Unhandled exception: {Method} {Path} [RequestId={RequestId}]",
            method,
            path,
            requestId);

        // Определяем тип ошибки и статус код
        int statusCode;
        ApiProblemDetails problemDetails;

        switch (exception)
        {
            // Ошибки валидации (ArgumentException и производные)
            case ArgumentException argEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                problemDetails = CreateValidationProblemDetails(argEx, path, requestId);
                break;

            // Ошибки "не найдено"
            case KeyNotFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                problemDetails = CreateNotFoundProblemDetails(path, requestId);
                break;

            // Ошибки API бирж (не пробрасываем детали наружу)
            case ExchangeApiException:
                statusCode = (int)HttpStatusCode.BadGateway;
                problemDetails = CreateServerProblemDetails(path, requestId, "Exchange API error");
                break;

            // Таймауты
            case TimeoutException:
                statusCode = (int)HttpStatusCode.GatewayTimeout;
                problemDetails = CreateTimeoutProblemDetails(path, requestId);
                break;

            // Операция отменена (не считаем ошибкой)
            case OperationCanceledException:
                statusCode = (int)HttpStatusCode.ServiceUnavailable;
                problemDetails = CreateServiceUnavailableProblemDetails(path, requestId);
                break;

            // Все остальные исключения — 500
            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                problemDetails = CreateServerProblemDetails(path, requestId);
                break;
        }

        // Формируем ответ
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(problemDetails, options);
        await context.Response.WriteAsync(json);
    }

    private static ApiValidationProblemDetails CreateValidationProblemDetails(
        ArgumentException ex,
        string path,
        string requestId)
    {
        return new ApiValidationProblemDetails
        {
            Type = "https://api.fundingmonitor.dev/errors/validation-error",
            Title = "Validation Error",
            Status = 400,
            Detail = ex.Message,
            Instance = path,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow,
            Errors = new Dictionary<string, string[]>
            {
                { ex.ParamName ?? "unknown", new[] { ex.Message } }
            }
        };
    }

    private static ApiNotFoundProblemDetails CreateNotFoundProblemDetails(
        string path,
        string requestId)
    {
        return new ApiNotFoundProblemDetails
        {
            Type = "https://api.fundingmonitor.dev/errors/not-found",
            Title = "Not Found",
            Status = 404,
            Detail = "The requested resource was not found",
            Instance = path,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow
        };
    }

    private static ApiServerProblemDetails CreateServerProblemDetails(
        string path,
        string requestId,
        string? detail = null)
    {
        return new ApiServerProblemDetails
        {
            Type = "https://api.fundingmonitor.dev/errors/server-error",
            Title = "Internal Server Error",
            Status = 500,
            Detail = detail ?? "An error occurred while processing your request",
            Instance = path,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow
        };
    }

    private static ApiTimeoutProblemDetails CreateTimeoutProblemDetails(
        string path,
        string requestId)
    {
        return new ApiTimeoutProblemDetails
        {
            Type = "https://api.fundingmonitor.dev/errors/timeout",
            Title = "Gateway Timeout",
            Status = 504,
            Detail = "The upstream server failed to respond in time",
            Instance = path,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow
        };
    }

    private static ApiServiceUnavailableProblemDetails CreateServiceUnavailableProblemDetails(
        string path,
        string requestId)
    {
        return new ApiServiceUnavailableProblemDetails
        {
            Type = "https://api.fundingmonitor.dev/errors/service-unavailable",
            Title = "Service Unavailable",
            Status = 503,
            Detail = "The service is temporarily unavailable",
            Instance = path,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow
        };
    }
}