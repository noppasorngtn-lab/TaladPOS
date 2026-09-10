using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.SalesOrders;

public interface ISalesOrderRepository
{
    Task AddAsync(SalesOrder order, CancellationToken cancellationToken);

    /// <summary>Includes Lines — callers (checkout/void) always need the full aggregate.</summary>
    Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
