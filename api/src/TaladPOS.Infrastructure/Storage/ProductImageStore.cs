using Microsoft.Extensions.Options;
using TaladPOS.Application.Products;

namespace TaladPOS.Infrastructure.Storage;

/// <summary>
/// Static-file product image storage under `wwwroot/product-images/` (research.md item 3) —
/// appropriate at single-store scale; no external blob-storage dependency required.
/// </summary>
public class ProductImageStore(IOptions<ProductImageStoreOptions> options) : IProductImageStore
{
    private readonly ProductImageStoreOptions _options = options.Value;

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_options.RootPath);

        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_options.RootPath, storedFileName);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return $"{_options.PublicBasePath}/{storedFileName}";
    }
}
