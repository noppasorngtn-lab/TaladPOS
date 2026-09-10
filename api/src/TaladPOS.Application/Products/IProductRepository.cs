using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>Matches by name-contains or exact barcode (FR-002).</summary>
    Task<(IReadOnlyList<Product> Items, int Total)> SearchAsync(
        string? search, bool includeInactive, int page, int pageSize, CancellationToken cancellationToken);
}
