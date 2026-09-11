using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Reports;

namespace TaladPOS.Api.Controllers.Reports;

[ApiController]
[Route("reports")]
[Authorize(Policy = "Admin")]
public class ReportsController(
    SalesSummaryQuery salesSummaryQuery,
    BestSellersQuery bestSellersQuery,
    SalesByStaffQuery salesByStaffQuery,
    StockLevelsQuery stockLevelsQuery,
    IWorkbookExportService workbookExportService) : ControllerBase
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    [HttpGet("sales-summary")]
    public async Task<ActionResult<SalesSummaryResponse>> SalesSummary(
        [FromQuery] string granularity, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var (rangeFrom, rangeTo) = RequireRange(from, to);
        var result = await salesSummaryQuery.ExecuteAsync(granularity, rangeFrom, rangeTo, cancellationToken);

        return Ok(new SalesSummaryResponse(result.Select(r => new SalesSummaryItemResponse(r.Period, r.TotalSales, r.OrderCount)).ToList()));
    }

    [HttpGet("best-sellers")]
    public async Task<ActionResult<BestSellersResponse>> BestSellers(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var (rangeFrom, rangeTo) = RequireRange(from, to);
        var result = await bestSellersQuery.ExecuteAsync(rangeFrom, rangeTo, limit, cancellationToken);

        return Ok(new BestSellersResponse(result.Select(r => new BestSellerResponse(r.ProductId, r.ProductName, r.QuantitySold, r.TotalAmount)).ToList()));
    }

    [HttpGet("sales-by-staff")]
    public async Task<ActionResult<SalesByStaffResponse>> SalesByStaff(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var (rangeFrom, rangeTo) = RequireRange(from, to);
        var result = await salesByStaffQuery.ExecuteAsync(rangeFrom, rangeTo, cancellationToken);

        return Ok(new SalesByStaffResponse(result.Select(r => new StaffSalesResponse(r.StaffId, r.StaffName, r.OrderCount, r.TotalSales)).ToList()));
    }

    [HttpGet("stock-levels")]
    public async Task<ActionResult<StockLevelsResponse>> StockLevels(CancellationToken cancellationToken)
    {
        var result = await stockLevelsQuery.ExecuteAsync(cancellationToken);
        return Ok(new StockLevelsResponse(result.Select(r => new StockLevelResponse(r.ProductId, r.ProductName, r.QuantityOnHand, r.LowStock)).ToList()));
    }

    // contracts/stock-export.md (feature 002-export-reports-sales-history, FR-001, FR-003, FR-006).
    // Reuses StockLevelsQuery so the on-screen list and the export can never disagree.
    [HttpGet("stock-levels/export")]
    public async Task<IActionResult> StockLevelsExport(CancellationToken cancellationToken)
    {
        var items = await stockLevelsQuery.ExecuteAsync(cancellationToken);

        string[] headers = ["Product name", "Quantity on hand", "Low stock"];
        var rows = items.Select(r => (IReadOnlyList<object?>)
            [r.ProductName, r.QuantityOnHand, r.LowStock ? "Low stock" : ""]);

        var bytes = workbookExportService.BuildXlsx("Stock levels", headers, rows);
        var fileName = $"stock-levels-{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}.xlsx";
        return File(bytes, XlsxContentType, fileName);
    }

    private static (DateOnly From, DateOnly To) RequireRange(DateOnly? from, DateOnly? to)
    {
        if (from is null || to is null)
        {
            throw new ArgumentException("Both 'from' and 'to' are required for this report.");
        }

        return (from.Value, to.Value);
    }
}
