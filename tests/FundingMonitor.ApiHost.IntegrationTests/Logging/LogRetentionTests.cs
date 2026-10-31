using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.Logging;

public sealed class LogRetentionTests
{
    [Fact]
    public void Full_queue_discards_without_blocking_and_flushes_after_release()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var factory = new LogFactory();
        var target = new WaitingTarget(entered, release);
        var wrapper = new AsyncTargetWrapper(target, 2, AsyncTargetWrapperOverflowAction.Discard)
        {
            Name = "queue", BatchSize = 1
        };
        var dropped = 0;
        wrapper.LogEventDropped += (_, _) => Interlocked.Increment(ref dropped);
        var config = new LoggingConfiguration();
        config.AddRuleForAllLevels(wrapper);
        factory.Configuration = config;
        try
        {
            var logger = factory.GetLogger("test");
            logger.Info("First");
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            for (var i = 0; i < 20; i++) logger.Info("Queued");
            Assert.True(dropped > 0);
        }
        finally { release.Set(); }
        factory.Flush(TimeSpan.FromSeconds(5));
        Assert.InRange(target.Written, 1, 3);
    }

    private sealed class WaitingTarget(ManualResetEventSlim entered, ManualResetEventSlim release) : Target
    {
        public int Written;
        protected override void Write(LogEventInfo logEvent)
        {
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Test target was not released");
            Interlocked.Increment(ref Written);
        }
    }
    [Fact]
    public void File_failure_does_not_escape_to_caller()
    {
        var directory = Path.Combine(Path.GetTempPath(), "funding-failure-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            using var factory = new LogFactory();
            var root = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(root, "FundingMonitor.sln"))) root = Directory.GetParent(root)!.FullName;
            var config = new XmlLoggingConfiguration(Path.Combine(root, "src/FundingMonitor.ApiHost/nlog.config"), factory);
            // An existing directory cannot be opened as a log file.
            config.AllTargets.OfType<FileTarget>().Single().FileName = directory;
            factory.Configuration = config;
            var error = Record.Exception(() =>
            {
                factory.GetLogger("FundingMonitor.Test").Error("Unavailable file");
                factory.Flush(TimeSpan.FromSeconds(5));
            });
            Assert.Null(error);
        }
        finally { Directory.Delete(directory, true); }
    }
    [Fact]
    public void Restart_appends_and_day_rotation_removes_expired_archives()
    {
        var directory = Path.Combine(Path.GetTempPath(), "funding-restart-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            var root = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(root, "FundingMonitor.sln"))) root = Directory.GetParent(root)!.FullName;
            void Write(string message)
            {
                using var factory = new LogFactory();
                var config = new XmlLoggingConfiguration(Path.Combine(root, "src/FundingMonitor.ApiHost/nlog.config"), factory);
                config.AllTargets.OfType<FileTarget>().Single().FileName = Path.Combine(directory, "app.jsonl");
                factory.Configuration = config;
                factory.GetLogger("FundingMonitor.Test").Info(message);
                factory.Flush(TimeSpan.FromSeconds(5));
            }
            Write("First run");
            Write("Second run");
            Assert.Equal(2, File.ReadAllLines(Path.Combine(directory, "app.jsonl")).Length);
            var old = DateTime.Now.AddDays(-10);
            var archive = Path.Combine(directory, "app_000.jsonl");
            File.WriteAllText(archive, "expired");
            File.SetLastWriteTime(archive, old);
            File.SetCreationTime(archive, old);
            File.SetLastWriteTime(Path.Combine(directory, "app.jsonl"), old);
            File.SetCreationTime(Path.Combine(directory, "app.jsonl"), old);
            Write("New day");
            Assert.False(File.Exists(archive), string.Join(",", Directory.GetFiles(directory)));
            Assert.Contains("New day", File.ReadAllText(Path.Combine(directory, "app.jsonl")));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void Rotation_limits_archives_and_preserves_unrelated_files()
    {
        var directory = Path.Combine(Path.GetTempPath(), "funding-retention-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            var root = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(root, "FundingMonitor.sln"))) root = Directory.GetParent(root)!.FullName;
            using var factory = new LogFactory();
            var config = new XmlLoggingConfiguration(Path.Combine(root, "src/FundingMonitor.ApiHost/nlog.config"), factory);
            var file = config.AllTargets.OfType<FileTarget>().Single();
            Assert.Equal(20, file.MaxArchiveFiles);
            Assert.Equal(7, file.MaxArchiveDays);
            Assert.Equal(20971520, file.ArchiveAboveSize);
            Assert.Equal(FileArchivePeriod.Day, file.ArchiveEvery);
            var wrapper = config.AllTargets.OfType<AsyncTargetWrapper>().Single();
            Assert.Equal(10000, wrapper.QueueLimit);
            Assert.Equal(AsyncTargetWrapperOverflowAction.Discard, wrapper.OverflowAction);
            file.FileName = Path.Combine(directory, "app.jsonl");
            file.ArchiveAboveSize = 100;
            file.MaxArchiveFiles = 2;
            File.WriteAllText(Path.Combine(directory, "unrelated.jsonl"), "keep");
            factory.Configuration = config;
            var logger = factory.GetLogger("FundingMonitor.Test");
            for (var i = 0; i < 10; i++)
            {
                logger.Info("Record {Number}", i);
                factory.Flush(TimeSpan.FromSeconds(5));
            }
            factory.Shutdown();
            Assert.InRange(Directory.GetFiles(directory, "app_*.jsonl").Length, 1, 2);
            Assert.Equal("keep", File.ReadAllText(Path.Combine(directory, "unrelated.jsonl")));
            Assert.Contains("Record 9", string.Join("\n", Directory.GetFiles(directory, "app*.jsonl").Select(File.ReadAllText)));
        }
        finally { Directory.Delete(directory, true); }
    }
}
