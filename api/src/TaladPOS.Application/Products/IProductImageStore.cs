namespace TaladPOS.Application.Products;

/// <summary>
/// Stores an uploaded product image and returns the relative URL to save on Product.ImageUrl.
/// Implemented in Infrastructure as static-file storage (research.md item 3); swappable for
/// cloud blob storage later without touching Domain/Application.
/// </summary>
public interface IProductImageStore
{
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken);
}
