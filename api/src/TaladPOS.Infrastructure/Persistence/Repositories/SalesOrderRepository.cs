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

    public async Task<(IReadOnlyList<SalesOrder> Items, int Total)> SearchAsync(
        DateOnly? from,
        DateOnly? to,
        Guid? staffId,
        Guid? memberId,
        SalesOrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.SalesOrders.AsQueryable();

        if (from.HasValue)
        {
            var fromUtc = new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(o => o.CreatedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toExclusiveUtc = new DateTimeOffset(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(o => o.CreatedAt < toExclusiveUtc);
        }

        if (staffId.HasValue)
        {
            query = query.Where(o => o.StaffId == staffId.Value);
        }

        if (memberId.HasValue)
        {
            query = query.Where(o => o.MemberId == memberId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
