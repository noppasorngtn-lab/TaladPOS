namespace TaladPOS.Domain.Entities;

/// <summary>
/// One product line within a <see cref="SalesOrder"/> (spec.md Key Entities: SalesOrderLine).
/// Construction is owned by <see cref="SalesOrder"/> — see the checkout flow added in
/// User Story 1 (specs/001-pos-system/tasks.md T025), which enforces Quantity &gt; 0 and
/// captures ProductNameSnapshot/UnitPriceSnapshot at sale time.
/// </summary>
public class SalesOrderLine
{
    public Guid Id { get; private set; }
    public Guid SalesOrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductNameSnapshot { get; private set; } = null!;
    public decimal UnitPriceSnapshot { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineDiscountAmount { get; private set; }

    private SalesOrderLine()
    {
    }

    internal static SalesOrderLine Create(Guid salesOrderId, Product product, int quantity) => new()
    {
        Id = Guid.NewGuid(),
        SalesOrderId = salesOrderId,
        ProductId = product.Id,
        ProductNameSnapshot = product.Name,
        UnitPriceSnapshot = product.Price,
        Quantity = quantity,
        LineDiscountAmount = 0m,
    };

    public decimal LineTotal => (UnitPriceSnapshot * Quantity) - LineDiscountAmount;
}
