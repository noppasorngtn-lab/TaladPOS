using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.SalesOrders;

public interface ISalesOrderRepository
{
    Task AddAsync(SalesOrder order, CancellationToken cancellationToken);

    /// <summary>Includes Lines — callers (checkout/void) always need the full aggregate.</summary>
    Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Sale history search (FR-026); any filter left null is not applied.</summary>
    Task<(IReadOnlyList<SalesOrder> Items, int Total)> SearchAsync(
        DateOnly? from,
        DateOnly? to,
        Guid? staffId,
        Guid? memberId,
        SalesOrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
