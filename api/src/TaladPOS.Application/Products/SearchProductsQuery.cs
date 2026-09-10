namespace TaladPOS.Application.Products;

public record ProductSummary(Guid Id, string Name, string? ImageUrl, decimal Price, int QuantityOnHand, string? Barcode, bool LowStock);

public record SearchProductsResult(IReadOnlyList<ProductSummary> Items, int Total);

/// <summary>Sales-screen product search by name or barcode (FR-002, contracts/products.md GET /products).</summary>
public class SearchProductsQuery(IProductRepository productRepository)
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<SearchProductsResult> ExecuteAsync(
        string? search, bool includeInactive, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? DefaultPageSize : pageSize;

        var (items, total) = await productRepository.SearchAsync(search, includeInactive, page, pageSize, cancellationToken);

        var summaries = items
            .Select(p => new ProductSummary(p.Id, p.Name, p.ImageUrl, p.Price, p.QuantityOnHand, p.Barcode, p.IsLowStock))
            .ToList();

        return new SearchProductsResult(summaries, total);
    }
}
