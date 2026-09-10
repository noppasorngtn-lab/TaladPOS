using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Products;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Api.Controllers.Products;

[ApiController]
[Route("products")]
[Authorize]
public class ProductsController(
    SearchProductsQuery searchProductsQuery,
    ManageProductUseCases manageProductUseCases,
    IProductImageStore productImageStore,
    IProductRepository productRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProductSearchResponse>> Search(
        [FromQuery] string? search,
        [FromQuery] bool includeInactive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // includeInactive is Admin-only (contracts/products.md); silently ignore it otherwise
        // rather than reject the request, since Cashiers legitimately call this same endpoint.
        var effectiveIncludeInactive = includeInactive && User.IsInRole(StaffRole.Admin);

        var result = await searchProductsQuery.ExecuteAsync(search, effectiveIncludeInactive, page, pageSize, cancellationToken);

        var items = result.Items
            .Select(p => new ProductSummaryResponse(p.Id, p.Name, p.ImageUrl, p.Price, p.QuantityOnHand, p.Barcode, p.LowStock))
            .ToList();

        return Ok(new ProductSearchResponse(items, result.Total));
    }

    [HttpGet("low-stock")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ProductSearchResponse>> LowStock(CancellationToken cancellationToken)
    {
        var products = await productRepository.GetLowStockAsync(cancellationToken);
        var items = products
            .Select(p => new ProductSummaryResponse(p.Id, p.Name, p.ImageUrl, p.Price, p.QuantityOnHand, p.Barcode, p.IsLowStock))
            .ToList();

        return Ok(new ProductSearchResponse(items, items.Count));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product {id} was not found.");

        return Ok(ToDetailResponse(product));
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ProductDetailResponse>> Create([FromForm] ProductFormRequest request, CancellationToken cancellationToken)
    {
        var imageUrl = await SaveImageIfProvidedAsync(request.Image, cancellationToken);

        var product = await manageProductUseCases.CreateAsync(
            new CreateProductRequest(request.Name, request.Price, request.QuantityOnHand, imageUrl, request.Barcode, request.LowStockThreshold),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToDetailResponse(product));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ProductDetailResponse>> Update(Guid id, [FromForm] ProductFormRequest request, CancellationToken cancellationToken)
    {
        var newImageUrl = await SaveImageIfProvidedAsync(request.Image, cancellationToken);
        string? imageUrl = newImageUrl;

        if (imageUrl is null)
        {
            var existing = await productRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"Product {id} was not found.");
            imageUrl = existing.ImageUrl;
        }

        var product = await manageProductUseCases.EditAsync(
            id,
            new EditProductRequest(request.Name, request.Price, request.QuantityOnHand, imageUrl, request.Barcode, request.LowStockThreshold),
            cancellationToken);

        return Ok(ToDetailResponse(product));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await manageProductUseCases.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    private async Task<string?> SaveImageIfProvidedAsync(IFormFile? image, CancellationToken cancellationToken)
    {
        if (image is null)
        {
            return null;
        }

        await using var stream = image.OpenReadStream();
        return await productImageStore.SaveAsync(stream, image.FileName, cancellationToken);
    }

    private static ProductDetailResponse ToDetailResponse(Product product) => new(
        product.Id, product.Name, product.ImageUrl, product.Price, product.QuantityOnHand,
        product.Barcode, product.LowStockThreshold, product.IsActive, product.IsLowStock);
}
