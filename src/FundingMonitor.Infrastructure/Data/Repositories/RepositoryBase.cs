using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.Infrastructure.Data.Repositories;

public abstract class RepositoryBase
{
    private readonly IDbContextFactory<FundingMonitorDbContext> _contextFactory;

    protected RepositoryBase(IDbContextFactory<FundingMonitorDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    protected async Task<FundingMonitorDbContext> CreateContextAsync(CancellationToken cancellationToken)
    {
        return await _contextFactory.CreateDbContextAsync(cancellationToken);
    }
}