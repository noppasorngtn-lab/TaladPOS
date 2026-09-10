using TaladPOS.Domain.Exceptions;

namespace TaladPOS.Domain.Entities;

/// <summary>
/// A sellable item in the single store's catalog (spec.md Key Entities: Product).
/// </summary>
public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? ImageUrl { get; private set; }
    public string? Barcode { get; private set; }
    public decimal Price { get; private set; }
    public int QuantityOnHand { get; private set; }
    public int? LowStockThreshold { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>Optimistic-concurrency token backed by PostgreSQL's xmin system column (research.md item 4).</summary>
    public uint RowVersion { get; private set; }

    private Product()
    {
    }

    public Product(string name, decimal price, int quantityOnHand, string? imageUrl = null, string? barcode = null, int? lowStockThreshold = null)
    {
        ValidateInvariants(name, price, quantityOnHand);

        Id = Guid.NewGuid();
        Name = name;
        Price = price;
        QuantityOnHand = quantityOnHand;
        ImageUrl = imageUrl;
        Barcode = barcode;
        LowStockThreshold = lowStockThreshold;
        IsActive = true;
    }

    public bool IsLowStock => LowStockThreshold.HasValue && QuantityOnHand <= LowStockThreshold.Value;

    /// <summary>Soft-delete: removes the product from sale without touching historical SalesOrderLine rows (FR-011).</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Updates editable fields in place (FR-010) without touching any past SalesOrderLine snapshot.</summary>
    public void Edit(string name, decimal price, int quantityOnHand, string? imageUrl, string? barcode, int? lowStockThreshold)
    {
        ValidateInvariants(name, price, quantityOnHand);

        Name = name;
        Price = price;
        QuantityOnHand = quantityOnHand;
        ImageUrl = imageUrl;
        Barcode = barcode;
        LowStockThreshold = lowStockThreshold;
    }

    /// <summary>
    /// Barcode uniqueness is a cross-aggregate rule, so the actual lookup lives in the repository
    /// (Application/Infrastructure); this pure function just decides what to do with the fact,
    /// keeping the rule itself unit-testable without a database (FR-014).
    /// </summary>
    public static void EnsureBarcodeIsAvailable(string? barcode, bool isBarcodeTakenByAnotherProduct)
    {
        if (!string.IsNullOrWhiteSpace(barcode) && isBarcodeTakenByAnotherProduct)
        {
            throw new ArgumentException($"Barcode '{barcode}' is already used by another product.", nameof(barcode));
        }
    }

    private static void ValidateInvariants(string name, decimal price, int quantityOnHand)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), price, "Price must be greater than 0.");
        }

        if (quantityOnHand < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityOnHand), quantityOnHand, "QuantityOnHand cannot be negative.");
        }
    }

    /// <summary>Business-rule stock guard for checkout (FR-005). Concurrent-checkout races are caught
    /// separately by the RowVersion optimistic-concurrency token at save time (research.md item 4).</summary>
    internal void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be greater than 0.");
        }

        if (quantity > QuantityOnHand)
        {
            throw new InsufficientStockException(Id, quantity, QuantityOnHand);
        }

        QuantityOnHand -= quantity;
    }

    /// <summary>Reverses a prior <see cref="DecreaseStock"/> when a SalesOrder is voided (FR-028).</summary>
    internal void RestoreStock(int quantity) => QuantityOnHand += quantity;
}
