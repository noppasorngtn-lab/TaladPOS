namespace TaladPOS.Api.Controllers.Reports;

public record SalesSummaryItemResponse(DateOnly Period, decimal TotalSales, int OrderCount);

public record SalesSummaryResponse(List<SalesSummaryItemResponse> Items);

public record BestSellerResponse(Guid ProductId, string ProductName, int QuantitySold, decimal TotalAmount);

public record BestSellersResponse(List<BestSellerResponse> Items);

public record StaffSalesResponse(Guid StaffId, string StaffName, int OrderCount, decimal TotalSales);

public record SalesByStaffResponse(List<StaffSalesResponse> Items);

public record StockLevelResponse(Guid ProductId, string ProductName, int QuantityOnHand, bool LowStock);

public record StockLevelsResponse(List<StockLevelResponse> Items);
