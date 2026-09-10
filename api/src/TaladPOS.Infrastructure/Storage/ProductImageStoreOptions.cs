namespace TaladPOS.Infrastructure.Storage;

public class ProductImageStoreOptions
{
    public const string SectionName = "ProductImages";

    /// <summary>Absolute filesystem path to write uploaded images to (resolved by api/ at startup from wwwroot).</summary>
    public string RootPath { get; set; } = null!;

    /// <summary>Public URL prefix the API serves these files under, e.g. "/product-images".</summary>
    public string PublicBasePath { get; set; } = "/product-images";
}
