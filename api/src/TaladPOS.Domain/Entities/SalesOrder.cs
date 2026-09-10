namespace TaladPOS.Domain.Entities;

/// <summary>
/// One completed (or voided) sales transaction — the aggregate root for a sale
/// (spec.md Key Entities: SalesOrder). The checkout/void behavior (adding lines,
/// computing totals, decrementing/restoring stock) is added in User Story 1
/// (specs/001-pos-system/tasks.md T025/T026) on top of this Foundational shape.
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
}
