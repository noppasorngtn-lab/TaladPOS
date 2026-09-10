using TaladPOS.Domain.Exceptions;

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
    /// (FR-005, research.md item 4), and computes totals. Promotion/member discounts are applied
    /// on top of this flat subtotal starting in User Story 4 (T053) — until then NetTotal equals
    /// SubtotalAmount.
    /// </summary>
    public static SalesOrder Checkout(Guid staffId, Guid? memberId, IReadOnlyCollection<(Product Product, int Quantity)> items)
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
            order._lines.Add(SalesOrderLine.Create(order.Id, product, quantity));
        }

        order.RecalculateTotals();
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

    private void RecalculateTotals()
    {
        SubtotalAmount = _lines.Sum(line => line.UnitPriceSnapshot * line.Quantity);
        NetTotal = SubtotalAmount - PromotionDiscountAmount - MemberDiscountAmount;
    }
}
