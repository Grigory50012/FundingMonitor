using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.Infrastructure.Data.Repositories;

public abstract class RepositoryBase
{
    protected readonly IDbContextFactory<FundingMonitorDbContext> ContextFactory;

    protected RepositoryBase(IDbContextFactory<FundingMonitorDbContext> contextFactory)
    {
        ContextFactory = contextFactory;
    }

    protected async Task<FundingMonitorDbContext> CreateContextAsync(CancellationToken cancellationToken)
    {
        return await ContextFactory.CreateDbContextAsync(cancellationToken);
    }
}