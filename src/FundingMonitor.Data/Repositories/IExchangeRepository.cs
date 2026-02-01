using FundingMonitor.Data.Entities;

namespace FundingMonitor.Data.Repositories;

public interface IExchangeRepository
{
    Task<Exchange?> GetByIdAsync(int id);
    Task<Exchange?> GetByNameAsync(string name);
    Task<List<Exchange>> GetAllAsync();
    Task<Exchange> AddAsync(Exchange exchange);
    Task UpdateAsync(Exchange exchange);
    Task DeleteAsync(int id);
}