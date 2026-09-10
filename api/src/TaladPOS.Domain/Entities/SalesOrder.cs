using TaladPOS.Domain.Exceptions;
using TaladPOS.Domain.Pricing;

namespace TaladPOS.Domain.Entities;

/// <summary>
/// One completed (or voided) sales transaction — the aggregate root for a sale
/// (spec.md Key Entities: SalesOrder).
/// </summary>
public class SalesOrder
{
    private readonly List<SalesOrderLine> _lines = [];

    public Guid Id { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid StaffId { get; private set; }
    public Guid? MemberId { get; private set; }
    public SalesOrderStatus Status { get; private set; }
    public decimal SubtotalAmount { get; private set; }
    public decimal PromotionDiscountAmount { get; private set; }
    public decimal MemberDiscountAmount { get; private set; }
    public decimal NetTotal { get; private set; }
    public DateTimeOffset? VoidedAt { get; private set; }
    public IReadOnlyCollection<SalesOrderLine> Lines => _lines.AsReadOnly();

    private SalesOrder()
    {
    }

    /// <summary>
    /// Creates a completed sale: adds one line per item, decrementing each product's stock
    /// (FR-005, research.md item 4), and computes totals. <paramref name="pricing"/> (from
    /// <see cref="SalesOrderPricingService"/>, User Story 4) supplies the promotion/member
    /// discount breakdown; when omitted, NetTotal equals SubtotalAmount (User Story 1 behavior).
    /// </summary>
    public static SalesOrder Checkout(
        Guid staffId, Guid? memberId, IReadOnlyCollection<(Product Product, int Quantity)> items, SalesOrderPricingResult? pricing = null)
    {
        if (items.Count == 0)
        {
            throw new ArgumentException("A sales order must contain at least one line.", nameof(items));
        }

        var order = new SalesOrder
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            StaffId = staffId,
            MemberId = memberId,
            Status = SalesOrderStatus.Completed,
        };

        foreach (var (product, quantity) in items)
        {
            product.DecreaseStock(quantity);
            var lineDiscount = pricing?.LineDiscountsByProductId.GetValueOrDefault(product.Id) ?? 0m;
            order._lines.Add(SalesOrderLine.Create(order.Id, product, quantity, lineDiscount));
        }

        order.SubtotalAmount = order._lines.Sum(line => line.UnitPriceSnapshot * line.Quantity);
        order.PromotionDiscountAmount = pricing?.PromotionDiscountAmount ?? 0m;
        order.MemberDiscountAmount = pricing?.MemberDiscountAmount ?? 0m;
        order.NetTotal = pricing?.NetTotal ?? order.SubtotalAmount;

        return order;
    }

    /// <summary>
    /// Transitions Completed → Voided (one-way, FR-028), only within the same calendar day the
    /// order was created (FR-027), restoring every line's quantity back to its product's stock.
    /// </summary>
    public void Void(DateTimeOffset requestedAt, IReadOnlyDictionary<Guid, Product> productsByLineProductId)
    {
        if (Status == SalesOrderStatus.Voided)
        {
            throw new AlreadyVoidedException(Id);
        }

        if (DateOnly.FromDateTime(requestedAt.UtcDateTime) != DateOnly.FromDateTime(CreatedAt.UtcDateTime))
        {
            throw new VoidWindowExpiredException(Id);
        }

        foreach (var line in _lines)
        {
            productsByLineProductId[line.ProductId].RestoreStock(line.Quantity);
        }

        Status = SalesOrderStatus.Voided;
        VoidedAt = requestedAt;
    }
}
