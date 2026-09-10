using Microsoft.EntityFrameworkCore;
using TaladPOS.Application.Reports;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Infrastructure.Persistence.Repositories;

public class ReportsRepository(TaladPOSDbContext dbContext) : IReportsRepository
{
    public async Task<IReadOnlyList<SalesSummaryPeriod>> GetSalesSummaryAsync(
        bool monthly, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var orders = await NonVoidedOrdersInRange(from, to)
            .Select(o => new { o.CreatedAt, o.NetTotal })
            .ToListAsync(cancellationToken);

        // Grouped in-memory (not via SQL date-trunc) — single-store sales volume doesn't need
        // it (plan.md Scale/Scope), and this keeps daily/monthly grouping database-agnostic.
        var grouped = monthly
            ? orders.GroupBy(o => new DateOnly(o.CreatedAt.UtcDateTime.Year, o.CreatedAt.UtcDateTime.Month, 1))
            : orders.GroupBy(o => DateOnly.FromDateTime(o.CreatedAt.UtcDateTime));

        return grouped
            .OrderBy(g => g.Key)
            .Select(g => new SalesSummaryPeriod(g.Key, g.Sum(o => o.NetTotal), g.Count()))
            .ToList();
    }

    public async Task<IReadOnlyList<BestSellerItem>> GetBestSellersAsync(
        DateOnly from, DateOnly to, int limit, CancellationToken cancellationToken)
    {
        var orders = await NonVoidedOrdersInRange(from, to).Include(o => o.Lines).ToListAsync(cancellationToken);

        return orders
            .SelectMany(o => o.Lines)
            .GroupBy(l => l.ProductId)
            .Select(g => new BestSellerItem(g.Key, g.First().ProductNameSnapshot, g.Sum(l => l.Quantity), g.Sum(l => l.LineTotal)))
            .OrderByDescending(item => item.TotalAmount)
            .Take(limit)
            .ToList();
    }

    public async Task<IReadOnlyList<StaffSalesItem>> GetSalesByStaffAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var orders = await NonVoidedOrdersInRange(from, to)
            .Select(o => new { o.StaffId, o.NetTotal })
            .ToListAsync(cancellationToken);

        var grouped = orders
            .GroupBy(o => o.StaffId)
            .Select(g => new { StaffId = g.Key, OrderCount = g.Count(), TotalSales = g.Sum(o => o.NetTotal) })
            .ToList();

        var staffIds = grouped.Select(g => g.StaffId).ToList();
        var namesById = await dbContext.Staff
            .Where(s => staffIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        return grouped
            .Select(g => new StaffSalesItem(g.StaffId, namesById.GetValueOrDefault(g.StaffId, "(unknown)"), g.OrderCount, g.TotalSales))
            .OrderByDescending(item => item.TotalSales)
            .ToList();
    }

    public async Task<IReadOnlyList<StockLevelItem>> GetStockLevelsAsync(CancellationToken cancellationToken) =>
        await dbContext.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new StockLevelItem(p.Id, p.Name, p.QuantityOnHand, p.LowStockThreshold != null && p.QuantityOnHand <= p.LowStockThreshold))
            .ToListAsync(cancellationToken);

    private IQueryable<SalesOrder> NonVoidedOrdersInRange(DateOnly from, DateOnly to)
    {
        var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toExclusiveUtc = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // FR-033: every report excludes Voided orders.
        return dbContext.SalesOrders.Where(o => o.Status != SalesOrderStatus.Voided && o.CreatedAt >= fromUtc && o.CreatedAt < toExclusiveUtc);
    }
}
