using TaladPOS.Application.Products;
using TaladPOS.Application.Promotions;
using TaladPOS.Domain.Pricing;

namespace TaladPOS.Application.SalesOrders;

public record PricingPreviewLineRequest(Guid ProductId, int Quantity);

public record PricingPreviewResult(decimal SubtotalAmount, decimal PromotionDiscountAmount, decimal MemberDiscountAmount, decimal NetTotal);

/// <summary>
/// Live cart total preview for the Sales screen (contracts/sales-orders.md GET /sales-orders/pricing-preview).
/// Runs the same <see cref="SalesOrderPricingService"/> as checkout without persisting or touching
/// stock, so the two can never disagree (research.md item 5).
/// </summary>
public class PricingPreviewQuery(IProductRepository productRepository, IPromotionRepository promotionRepository)
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

        var pricingLines = lines.Select(line => new PricingLine(line.ProductId, productsById[line.ProductId].Price, line.Quantity)).ToList();
        var promotions = await promotionRepository.GetActiveAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var result = SalesOrderPricingService.Calculate(pricingLines, promotions, today, memberLinked: memberId.HasValue);

        return new PricingPreviewResult(result.SubtotalAmount, result.PromotionDiscountAmount, result.MemberDiscountAmount, result.NetTotal);
    }
}
