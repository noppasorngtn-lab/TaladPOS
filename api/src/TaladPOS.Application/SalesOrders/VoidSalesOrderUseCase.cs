using TaladPOS.Application.Members;
using TaladPOS.Application.Products;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.SalesOrders;

/// <summary>Voids a same-day completed order, restores stock, and reverses any member accrual (FR-027, FR-028).</summary>
public class VoidSalesOrderUseCase(
    ISalesOrderRepository salesOrderRepository, IProductRepository productRepository, IMemberRepository memberRepository)
{
    public async Task<SalesOrder> ExecuteAsync(Guid salesOrderId, CancellationToken cancellationToken)
    {
        var order = await salesOrderRepository.GetByIdAsync(salesOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"SalesOrder {salesOrderId} was not found.");

        var productIds = order.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productsById = products.ToDictionary(p => p.Id);

        // Void first — it throws on an already-voided or same-day-expired order, and reversing
        // the member credit beforehand would leave a mutated Member on the tracked graph if it did.
        order.Void(DateTimeOffset.UtcNow, productsById);

        if (order.MemberId.HasValue)
        {
            var member = await memberRepository.GetByIdAsync(order.MemberId.Value, cancellationToken);
            member?.ReverseCredit(order.NetTotal);
        }

        await salesOrderRepository.SaveChangesAsync(cancellationToken);
        return order;
    }
}
