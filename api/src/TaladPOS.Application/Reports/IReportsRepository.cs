namespace TaladPOS.Application.Reports;

public record SalesSummaryPeriod(DateOnly Period, decimal TotalSales, int OrderCount);

public record BestSellerItem(Guid ProductId, string ProductName, int QuantitySold, decimal TotalAmount);

public record StaffSalesItem(Guid StaffId, string StaffName, int OrderCount, decimal TotalSales);

public record StockLevelItem(Guid ProductId, string ProductName, int QuantityOnHand, bool LowStock);

/// <summary>
/// Read-only aggregations for the Reports screen (FR-029–FR-032). All sales aggregations
/// exclude Voided orders (FR-033).
/// </summary>
public interface IReportsRepository
{
    Task<IReadOnlyList<SalesSummaryPeriod>> GetSalesSummaryAsync(bool monthly, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task<IReadOnlyList<BestSellerItem>> GetBestSellersAsync(DateOnly from, DateOnly to, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<StaffSalesItem>> GetSalesByStaffAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task<IReadOnlyList<StockLevelItem>> GetStockLevelsAsync(CancellationToken cancellationToken);
}
