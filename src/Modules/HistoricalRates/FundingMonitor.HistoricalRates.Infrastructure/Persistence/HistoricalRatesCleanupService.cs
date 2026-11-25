using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.HistoricalRates.Infrastructure.Persistence;

internal sealed class HistoricalRatesCleanupService(
    IDbContextFactory<HistoricalRatesDbContext> dbContextFactory,
    IOptions<HistoricalRatesCleanupOptions> options,
    TimeProvider timeProvider,
    ILogger<HistoricalRatesCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan MoscowOffset = TimeSpan.FromHours(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = timeProvider.GetUtcNow();
            var nextRun = GetNextRun(now, settings.RunAtMoscowTime);
            await Task.Delay(nextRun - now, timeProvider, stoppingToken);

            try
            {
                await using var dbContext = await dbContextFactory.CreateDbContextAsync(stoppingToken);
                var cutoff = timeProvider.GetUtcNow().AddDays(-settings.RetentionDays);
                var deleted = await dbContext.HistoricalFundingRates
                    .Where(rate => rate.FundingTime < cutoff)
                    .ExecuteDeleteAsync(stoppingToken);
                logger.LogInformation("Historical rates cleanup deleted {DeletedCount} records", deleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Historical rates cleanup failed");
            }
        }
    }

    internal static DateTimeOffset GetNextRun(DateTimeOffset now, TimeOnly runAt)
    {
        var moscowDate = now.ToOffset(MoscowOffset).Date;
        var scheduled = new DateTimeOffset(moscowDate.Add(runAt.ToTimeSpan()), MoscowOffset);
        return scheduled > now ? scheduled : scheduled.AddDays(1);
    }
}
