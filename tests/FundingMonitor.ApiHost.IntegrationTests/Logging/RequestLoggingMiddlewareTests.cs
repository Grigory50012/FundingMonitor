using FundingMonitor.ApiHost.Shared.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.Logging;

public sealed class RequestLoggingMiddlewareTests
{
    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(500)]
    public async Task Logs_final_status_once(int status)
    {
        var context = new DefaultHttpContext { TraceIdentifier = "request-test" };
        context.Request.Path = "/test";
        context.Request.QueryString = new QueryString("?secret=hidden");
        context.Response.Body = new MemoryStream();
        var logger = new RecordingLogger<RequestLoggingMiddleware>();
        var exceptionLogger = new RecordingLogger<ExceptionHandlingMiddleware>();
        var handler = new ExceptionHandlingMiddleware(_ => status switch
        {
            400 => throw new ArgumentException("Invalid"),
            404 => throw new KeyNotFoundException(),
            500 => throw new InvalidOperationException(),
            _ => Task.CompletedTask
        }, exceptionLogger);
        await new RequestLoggingMiddleware(handler.InvokeAsync, logger).InvokeAsync(context);
        Assert.Empty(exceptionLogger.Events);
        Assert.Equal(status >= 400, Assert.Single(logger.Exceptions) is not null);
        var entry = Assert.Single(logger.Events).ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal(status, entry["StatusCode"]);
        Assert.Equal("/test", entry["Path"]);
        Assert.True((double)entry["DurationMs"]! >= 0);
    }
}
