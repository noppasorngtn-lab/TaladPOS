namespace TaladPOS.Api.Controllers.Products;

public record ProductSummaryResponse(Guid Id, string Name, string? ImageUrl, decimal Price, int QuantityOnHand, string? Barcode, bool LowStock);

public record ProductSearchResponse(List<ProductSummaryResponse> Items, int Total);
