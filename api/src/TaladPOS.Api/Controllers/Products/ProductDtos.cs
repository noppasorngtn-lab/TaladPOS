namespace TaladPOS.Api.Controllers.Products;

public record ProductSummaryResponse(Guid Id, string Name, string? ImageUrl, decimal Price, int QuantityOnHand, string? Barcode, bool LowStock);

public record ProductSearchResponse(List<ProductSummaryResponse> Items, int Total);

public record ProductDetailResponse(
    Guid Id,
    string Name,
    string? ImageUrl,
    decimal Price,
    int QuantityOnHand,
    string? Barcode,
    int? LowStockThreshold,
    bool IsActive,
    bool LowStock);

/// <summary>Bound from multipart/form-data (contracts/products.md POST/PUT /products).</summary>
public class ProductFormRequest
{
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public int QuantityOnHand { get; set; }
    public string? Barcode { get; set; }
    public int? LowStockThreshold { get; set; }
    public IFormFile? Image { get; set; }
}
