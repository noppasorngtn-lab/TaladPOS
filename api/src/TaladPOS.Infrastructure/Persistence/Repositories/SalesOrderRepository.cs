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
        var query = ApplyFilters(dbContext.SalesOrders, from, to, staffId, memberId, status);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    // Feature 002-export-reports-sales-history, User Story 2 (FR-004/FR-005/FR-007).
    // Same filters as SearchAsync, minus paging, and name-resolved the same way
    // ReportsRepository.GetSalesByStaffAsync already does (research.md item 4) so the Application
    // layer never has to join Staff/Members itself.
    public async Task<IReadOnlyList<SalesHistoryExportRow>> SearchAllForExportAsync(
        DateOnly? from,
        DateOnly? to,
        Guid? staffId,
        Guid? memberId,
        SalesOrderStatus? status,
        CancellationToken cancellationToken)
    {
        var query = ApplyFilters(dbContext.SalesOrders, from, to, staffId, memberId, status);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new { o.CreatedAt, o.StaffId, o.MemberId, o.NetTotal, o.Status })
            .ToListAsync(cancellationToken);

        var staffIds = orders.Select(o => o.StaffId).Distinct().ToList();
        var staffNamesById = await dbContext.Staff
            .Where(s => staffIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var memberIds = orders.Where(o => o.MemberId.HasValue).Select(o => o.MemberId!.Value).Distinct().ToList();
        var memberNamesById = await dbContext.Members
            .Where(m => memberIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name, cancellationToken);

        return orders
            .Select(o => new SalesHistoryExportRow(
                o.CreatedAt,
                staffNamesById.GetValueOrDefault(o.StaffId, "(unknown)"),
                o.MemberId.HasValue ? memberNamesById.GetValueOrDefault(o.MemberId.Value, "(unknown)") : null,
                o.NetTotal,
                o.Status))
            .ToList();
    }

    private static IQueryable<SalesOrder> ApplyFilters(
        IQueryable<SalesOrder> query, DateOnly? from, DateOnly? to, Guid? staffId, Guid? memberId, SalesOrderStatus? status)
    {
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

        return query;
    }
}
