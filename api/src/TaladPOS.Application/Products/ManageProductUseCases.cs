using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.Products;

public record CreateProductRequest(string Name, decimal Price, int QuantityOnHand, string? ImageUrl, string? Barcode, int? LowStockThreshold);

public record EditProductRequest(string Name, decimal Price, int QuantityOnHand, string? ImageUrl, string? Barcode, int? LowStockThreshold);

/// <summary>Product create/edit/soft-delete for the Stock Management screen (FR-009–FR-011, FR-014).</summary>
public class ManageProductUseCases(IProductRepository productRepository)
{
    public async Task<Product> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        await EnsureBarcodeIsAvailableAsync(request.Barcode, excludeProductId: null, cancellationToken);

        var product = new Product(request.Name, request.Price, request.QuantityOnHand, request.ImageUrl, request.Barcode, request.LowStockThreshold);

        await productRepository.AddAsync(product, cancellationToken);
        await productRepository.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<Product> EditAsync(Guid id, EditProductRequest request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product {id} was not found.");

        await EnsureBarcodeIsAvailableAsync(request.Barcode, excludeProductId: id, cancellationToken);

        product.Edit(request.Name, request.Price, request.QuantityOnHand, request.ImageUrl, request.Barcode, request.LowStockThreshold);
        await productRepository.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product {id} was not found.");

        product.Deactivate();
        await productRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBarcodeIsAvailableAsync(string? barcode, Guid? excludeProductId, CancellationToken cancellationToken)
    {
        var isTaken = !string.IsNullOrWhiteSpace(barcode)
            && await productRepository.BarcodeExistsAsync(barcode, excludeProductId, cancellationToken);
        Product.EnsureBarcodeIsAvailable(barcode, isTaken);
    }
}
