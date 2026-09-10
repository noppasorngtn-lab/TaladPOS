using TaladPOS.Application.Members;
using TaladPOS.Application.Products;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.SalesOrders;

public record CheckoutLineRequest(Guid ProductId, int Quantity);

/// <summary>Validates lines, loads products, and delegates to the Domain aggregate (FR-002–FR-006).</summary>
public class CheckoutUseCase(
    IProductRepository productRepository, ISalesOrderRepository salesOrderRepository, IMemberRepository memberRepository)
{
    public async Task<SalesOrder> ExecuteAsync(
        Guid staffId, Guid? memberId, IReadOnlyList<CheckoutLineRequest> lines, CancellationToken cancellationToken)
    {
        if (lines.Count == 0)
        {
            throw new ArgumentException("A sales order must contain at least one line.", nameof(lines));
        }

        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productsById = products.ToDictionary(p => p.Id);

        var missingIds = productIds.Where(id => !productsById.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new ArgumentException($"Unknown product id(s): {string.Join(", ", missingIds)}", nameof(lines));
        }

        // Loaded before Checkout() so an unknown memberId (contracts/sales-orders.md 422) fails
        // before any stock is decremented.
        Member? member = null;
        if (memberId.HasValue)
        {
            member = await memberRepository.GetByIdAsync(memberId.Value, cancellationToken)
                ?? throw new ArgumentException($"Unknown member id: {memberId.Value}", nameof(memberId));
        }

        var items = lines.Select(line => (productsById[line.ProductId], line.Quantity)).ToList();
        var order = SalesOrder.Checkout(staffId, memberId, items);

        // Accrual happens on the order's NetTotal, not the flat subtotal (FR-018, data-model.md) —
        // they're equal until User Story 4 wires in promotion/member discounts (T053).
        member?.Credit(order.NetTotal);

        await salesOrderRepository.AddAsync(order, cancellationToken);
        await salesOrderRepository.SaveChangesAsync(cancellationToken);

        return order;
    }
}
