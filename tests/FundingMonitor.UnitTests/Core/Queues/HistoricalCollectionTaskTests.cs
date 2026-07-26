using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Queues;
using Xunit;

namespace FundingMonitor.UnitTests.Core.Queues;

public sealed class HistoricalCollectionTaskTests
{
    [Fact]
    public void Constructor_AssignsStableMessageEnvelope()
    {
        var taskId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 7, 26, 18, 0, 0, DateTimeKind.Utc);

        var task = new HistoricalCollectionTask(
            taskId,
            ExchangeType.Binance,
            "BTC-USDT",
            createdAt,
            RetryCount: 2);

        Assert.Equal(taskId, task.TaskId);
        Assert.Equal(ExchangeType.Binance, task.Exchange);
        Assert.Equal("BTC-USDT", task.NormalizedSymbol);
        Assert.Equal(createdAt, task.CreatedAt);
        Assert.Equal(2, task.RetryCount);
    }
}
