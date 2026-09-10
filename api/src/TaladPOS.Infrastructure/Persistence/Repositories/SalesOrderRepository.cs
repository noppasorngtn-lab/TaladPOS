using Microsoft.EntityFrameworkCore;
using TaladPOS.Application.SalesOrders;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Infrastructure.Persistence.Repositories;

public class SalesOrderRepository(TaladPOSDbContext dbContext) : ISalesOrderRepository
{
    public Task AddAsync(SalesOrder order, CancellationToken cancellationToken)
    {
        dbContext.SalesOrders.Add(order);
        return Task.CompletedTask;
    }

    public Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.SalesOrders.Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
