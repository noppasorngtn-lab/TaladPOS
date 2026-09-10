using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>Matches by name-contains or exact barcode (FR-002).</summary>
    Task<(IReadOnlyList<Product> Items, int Total)> SearchAsync(
        string? search, bool includeInactive, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Active products at or below their LowStockThreshold (FR-013).</summary>
    Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken cancellationToken);

    /// <summary>Backs <see cref="Product.EnsureBarcodeIsAvailable"/> (FR-014) — excludeProductId lets an
    /// edit keep its own existing barcode without flagging itself as a conflict.</summary>
    Task<bool> BarcodeExistsAsync(string barcode, Guid? excludeProductId, CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
