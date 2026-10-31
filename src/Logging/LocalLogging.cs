using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;

namespace FundingMonitor.Hosting;

// Host configuration only; modules continue to depend on ILogger<T>.
internal static class LocalLogging
{
    public static LogFactory Create(IConfiguration configuration, string application, string configFile)
    {
        var directory = Path.GetFullPath(configuration["Logging:Directory"] ?? $"logs/{application}", AppContext.BaseDirectory);
        var factory = new LogFactory();
        var config = new XmlLoggingConfiguration(Path.Combine(AppContext.BaseDirectory, configFile), factory);
        config.Variables["logDirectory"] = directory;
        factory.Configuration = config;
        factory.GetLogger("FundingMonitor.Hosting").Info("Logging initialized at {LogDirectory}", directory);
        return factory;
    }

    public static void Add(ILoggingBuilder logging, LogFactory factory)
    {
        logging.ClearProviders();
        logging.AddFilter<NLogLoggerProvider>(null, Microsoft.Extensions.Logging.LogLevel.Trace);
        logging.AddProvider(new NLogLoggerProvider(new NLogProviderOptions
        {
            CaptureEventId = EventIdCaptureType.None,
            ShutdownOnDispose = false,
            RemoveLoggerFactoryFilter = true
        }, factory));
    }
}
