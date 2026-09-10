using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Products;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Api.Controllers.Products;

[ApiController]
[Route("products")]
[Authorize]
public class ProductsController(SearchProductsQuery searchProductsQuery) : ControllerBase
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
}
