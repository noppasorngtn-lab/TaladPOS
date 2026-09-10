using TaladPOS.Domain.Entities;

namespace TaladPOS.Domain.Pricing;

public record PricingLine(Guid ProductId, decimal UnitPrice, int Quantity);

public record SalesOrderPricingResult(
    decimal SubtotalAmount,
    decimal PromotionDiscountAmount,
    decimal MemberDiscountAmount,
    decimal NetTotal,
    IReadOnlyDictionary<Guid, decimal> LineDiscountsByProductId);

/// <summary>
/// Combined promotion + member discount calculation (FR-020–FR-024, FR-036), shared by
/// checkout (CheckoutUseCase) and the cart preview (PricingPreviewQuery) so the two can never
/// disagree (research.md item 5). Date-window filtering (FR-023) happens here via
/// <see cref="Promotion.CoversDate"/> rather than in the caller, so callers may pass every
/// active promotion regardless of date.
/// </summary>
public static class SalesOrderPricingService
{
    public static SalesOrderPricingResult Calculate(
        IReadOnlyCollection<PricingLine> lines,
        IReadOnlyCollection<Promotion> promotions,
        DateOnly asOfDate,
        bool memberLinked)
    {
        var applicable = promotions.Where(p => p.CoversDate(asOfDate)).ToList();

        var subtotal = lines.Sum(line => line.UnitPrice * line.Quantity);

        // First PerProduct promotion found per product wins — overlapping promotions on the
        // same product aren't a case this system supports configuring.
        var perProductPromotionsByProduct = applicable
            .Where(p => p.Scope == PromotionScope.PerProduct)
            .GroupBy(p => p.ProductId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var lineDiscounts = new Dictionary<Guid, decimal>();
        var promotionDiscount = 0m;
        foreach (var line in lines)
        {
            var discount = perProductPromotionsByProduct.TryGetValue(line.ProductId, out var promotion)
                ? line.UnitPrice * line.Quantity * promotion.DiscountPercent / 100m
                : 0m;
            lineDiscounts[line.ProductId] = discount;
            promotionDiscount += discount;
        }

        var wholeBillPromotion = applicable.FirstOrDefault(p => p.Scope == PromotionScope.WholeBill);
        if (wholeBillPromotion is not null)
        {
            promotionDiscount += subtotal * wholeBillPromotion.DiscountPercent / 100m;
        }

        // FR-036: sequential discounting — member discount applies to what's left after the
        // promotion discount, not the original subtotal.
        var afterPromotion = Math.Max(0m, subtotal - promotionDiscount);

        var memberDiscount = 0m;
        if (memberLinked)
        {
            var memberPromotion = applicable.FirstOrDefault(p => p.Scope == PromotionScope.MemberDiscount);
            if (memberPromotion is not null)
            {
                memberDiscount = afterPromotion * memberPromotion.DiscountPercent / 100m;
            }
        }

        var netTotal = Math.Max(0m, afterPromotion - memberDiscount);

        return new SalesOrderPricingResult(subtotal, promotionDiscount, memberDiscount, netTotal, lineDiscounts);
    }
}
