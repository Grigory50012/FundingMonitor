using FundingMonitor.ApiHost.Shared.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.Logging;

public sealed class ExceptionLoggingTests
{
    [Fact]
    public async Task Unexpected_cancellation_remains_an_error()
    {
        var logger = new RecordingLogger<ExceptionHandlingMiddleware>();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        await new ExceptionHandlingMiddleware(_ => throw new OperationCanceledException(), logger).InvokeAsync(context);
        Assert.Contains(LogLevel.Error, logger.Levels);
        Assert.False(context.Items.ContainsKey("FundingMonitor.RequestCancelled"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Expected_exception_is_not_an_error(bool cancelled)
    {
        var logger = new RecordingLogger<ExceptionHandlingMiddleware>();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        if (cancelled) context.RequestAborted = new CancellationToken(true);
        var middleware = new ExceptionHandlingMiddleware(_ => throw (cancelled
            ? new OperationCanceledException() : new ArgumentException("Invalid input")), logger);
        await middleware.InvokeAsync(context);
        Assert.DoesNotContain(logger.Levels, level => level >= LogLevel.Error);
        Assert.Equal(cancelled ? 503 : 400, context.Response.StatusCode);
    }
}

internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<Exception?> Exceptions { get; } = [];
    public List<LogLevel> Levels { get; } = [];
    public List<IReadOnlyList<KeyValuePair<string, object?>>> Events { get; } = [];
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Levels.Add(logLevel);
        Exceptions.Add(exception);
        Events.Add(((IEnumerable<KeyValuePair<string, object?>>)state!).ToArray());
    }
}
