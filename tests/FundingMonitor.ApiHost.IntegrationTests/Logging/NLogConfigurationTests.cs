using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.Text.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.Logging;

public sealed class NLogConfigurationTests
{
    [Theory]
    [InlineData("FundingMonitor.ApiHost/nlog.config")]
    [InlineData("FundingMonitor.DatabaseMigrator/nlog.migrator.config")]
    public void File_preserves_structured_fields_and_multiline_exception(string configuration)
    {
        var directory = Path.Combine(Path.GetTempPath(), "funding-logs-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            using var factory = new LogFactory();
            var root = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(root, "FundingMonitor.sln")))
                root = Directory.GetParent(root)!.FullName;
            var config = new XmlLoggingConfiguration(Path.Combine(root, "src", configuration), factory);
            foreach (var target in config.AllTargets.OfType<FileTarget>())
                target.FileName = Path.Combine(directory, "app.jsonl");
            factory.Configuration = config;
            using var provider = new NLogLoggerProvider(new NLogProviderOptions { CaptureEventId = EventIdCaptureType.None }, factory);
            using var microsoftFactory = LoggerFactory.Create(builder => { builder.AddFilter<NLogLoggerProvider>(null, Microsoft.Extensions.Logging.LogLevel.Trace); builder.AddProvider(provider); });
            var logger = microsoftFactory.CreateLogger("FundingMonitor.Test");
            using (logger.BeginScope(new Dictionary<string, object?> { ["Exchange"] = "Binance", ["EventId"] = "business-event" }))
            {
                logger.LogWarning(new TimeoutException("Line one\nline two"), "Retry {Attempt}", 2);
                logger.LogDebug("Hidden detail");
            }
            factory.Flush(TimeSpan.FromSeconds(5));
            factory.Shutdown();
            using var record = JsonDocument.Parse(File.ReadAllLines(Path.Combine(directory, "app.jsonl")).Single());
            Assert.Equal("Binance", record.RootElement.GetProperty("Exchange").GetString());
            Assert.Equal("business-event", record.RootElement.GetProperty("EventId").GetString());
            Assert.Equal(2, record.RootElement.GetProperty("Attempt").GetInt32());
            Assert.Contains("TimeoutException", record.RootElement.GetProperty("Exception").GetString());
            Assert.EndsWith("Z", record.RootElement.GetProperty("Timestamp").GetString());
        }
        finally { Directory.Delete(directory, true); }
    }
}
