using Microsoft.EntityFrameworkCore;
using TaladPOS.Application.Products;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Infrastructure.Persistence.Repositories;

public class ProductRepository(TaladPOSDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Products.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await dbContext.Products.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int Total)> SearchAsync(
        string? search, bool includeInactive, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%") || p.Barcode == search);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken cancellationToken) =>
        await dbContext.Products
            .Where(p => p.IsActive && p.LowStockThreshold != null && p.QuantityOnHand <= p.LowStockThreshold)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> BarcodeExistsAsync(string barcode, Guid? excludeProductId, CancellationToken cancellationToken)
    {
        var query = dbContext.Products.Where(p => p.Barcode == barcode);
        if (excludeProductId.HasValue)
        {
            query = query.Where(p => p.Id != excludeProductId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        dbContext.Products.Add(product);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
