using System.Diagnostics;

namespace FundingMonitor.ApiHost.Shared.Middleware;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    internal static readonly object ActiveKey = new();
    internal static readonly object ExceptionKey = new();
    public async Task InvokeAsync(HttpContext context)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["RequestId"] = Activity.Current?.Id ?? context.TraceIdentifier
        });
        var started = Stopwatch.GetTimestamp();
        context.Items[ActiveKey] = true;
        var completed = false;
        try
        {
            await next(context);
            completed = true;
        }
        catch (Exception exception)
        {
            context.Items[ExceptionKey] = exception;
            throw;
        }
        finally
        {
            var cancelled = context.RequestAborted.IsCancellationRequested || context.Items.ContainsKey("FundingMonitor.RequestCancelled");
            var level = cancelled ? LogLevel.Information
                : !completed || context.Response.StatusCode >= 500 ? LogLevel.Error
                : context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            logger.Log(level, cancelled ? null : context.Items[ExceptionKey] as Exception,
                "HTTP {Method} {Path} returned {StatusCode} in {DurationMs} ms; outcome {Outcome}",
                context.Request.Method, context.Request.Path.Value, context.Response.StatusCode,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                cancelled ? "Cancelled" : completed ? "Completed" : "Aborted");
        }
    }
}
