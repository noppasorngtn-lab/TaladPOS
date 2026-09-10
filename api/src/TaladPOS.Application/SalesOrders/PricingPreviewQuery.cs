using TaladPOS.Application.Products;

namespace TaladPOS.Application.SalesOrders;

public record PricingPreviewLineRequest(Guid ProductId, int Quantity);

public record PricingPreviewResult(decimal SubtotalAmount, decimal PromotionDiscountAmount, decimal MemberDiscountAmount, decimal NetTotal);

/// <summary>
/// Live cart total preview for the Sales screen (contracts/sales-orders.md GET /sales-orders/pricing-preview).
/// Runs the same product-price math as checkout without persisting or touching stock. Promotion and
/// member-discount amounts are wired in by User Story 4 (T053, SalesOrderPricingService) — until then
/// they are always 0 and NetTotal equals SubtotalAmount.
/// </summary>
public class PricingPreviewQuery(IProductRepository productRepository)
{
    public async Task<PricingPreviewResult> ExecuteAsync(
        Guid? memberId, IReadOnlyList<PricingPreviewLineRequest> lines, CancellationToken cancellationToken)
    {
        if (lines.Count == 0)
        {
            throw new ArgumentException("A pricing preview must contain at least one line.", nameof(lines));
        }

        foreach (var line in lines)
        {
            if (line.Quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lines), line.Quantity, "Quantity must be greater than 0.");
            }
        }

        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        var productsById = (await productRepository.GetByIdsAsync(productIds, cancellationToken)).ToDictionary(p => p.Id);

        var missingIds = productIds.Where(id => !productsById.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new ArgumentException($"Unknown product id(s): {string.Join(", ", missingIds)}", nameof(lines));
        }

        var subtotal = lines.Sum(line => productsById[line.ProductId].Price * line.Quantity);
        return new PricingPreviewResult(subtotal, PromotionDiscountAmount: 0m, MemberDiscountAmount: 0m, NetTotal: subtotal);
    }
}
