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
}
