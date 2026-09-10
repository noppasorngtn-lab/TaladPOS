using TaladPOS.Application.Products;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.SalesOrders;

/// <summary>Voids a same-day completed order and restores stock (FR-027, FR-028).</summary>
public class VoidSalesOrderUseCase(ISalesOrderRepository salesOrderRepository, IProductRepository productRepository)
{
    public async Task<SalesOrder> ExecuteAsync(Guid salesOrderId, CancellationToken cancellationToken)
    {
        var order = await salesOrderRepository.GetByIdAsync(salesOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"SalesOrder {salesOrderId} was not found.");

        var productIds = order.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productsById = products.ToDictionary(p => p.Id);

        order.Void(DateTimeOffset.UtcNow, productsById);

        await salesOrderRepository.SaveChangesAsync(cancellationToken);
        return order;
    }
}
