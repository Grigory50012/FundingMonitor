using FundingMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.Data.Repositories;

public class ExchangeRepository : IExchangeRepository
{
    private readonly AppDbContext _dbContext;
    
    public ExchangeRepository(AppDbContext context)
    {
        _dbContext = context;
    }
    
    public async Task<Exchange?> GetByIdAsync(int id)
    {
        return await _dbContext.Exchanges.FindAsync(id);
    }

    public async Task<Exchange?> GetByNameAsync(string name)
    {
        return await _dbContext.Exchanges
            .FirstOrDefaultAsync(e => e.Name == name);
    }

    public async Task<List<Exchange>> GetAllAsync()
    {
        return await _dbContext.Exchanges
            .Where(e => e.IsActive)
            .ToListAsync();
    }

    public async Task<Exchange> AddAsync(Exchange exchange)
    {
        _dbContext.Exchanges.Add(exchange);
        await _dbContext.SaveChangesAsync();
        return exchange;
    }

    public async Task UpdateAsync(Exchange exchange)
    {
        _dbContext.Exchanges.Update(exchange);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var exchange = await GetByIdAsync(id);
        if (exchange != null)
        {
            exchange.IsActive = false;
            await UpdateAsync(exchange);
        }
    }
}