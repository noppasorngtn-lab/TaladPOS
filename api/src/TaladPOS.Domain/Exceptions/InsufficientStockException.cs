namespace TaladPOS.Domain.Exceptions;

/// <summary>Raised when a requested sale quantity exceeds a product's current stock (FR-005).</summary>
public class InsufficientStockException(Guid productId, int requestedQuantity, int quantityOnHand)
    : DomainConflictException(
        "insufficient_stock",
        $"Product {productId} has {quantityOnHand} on hand, which is less than the requested quantity of {requestedQuantity}.")
{
    public Guid ProductId { get; } = productId;
    public int RequestedQuantity { get; } = requestedQuantity;
    public int QuantityOnHand { get; } = quantityOnHand;
}
