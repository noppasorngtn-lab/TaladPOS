using System.Net.Http.Json;
using FluentAssertions;
using TaladPOS.Api.IntegrationTests.Helpers;

namespace TaladPOS.Api.IntegrationTests;

public class ReportsTests(TaladPosApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Sales_summary_and_best_sellers_exclude_voided_orders()
    {
        var admin = await CreateAdminClientAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var form = MultipartFormBuilder.ProductForm($"ReportItem-{Guid.NewGuid():N}", 50m, 20);
        var product = await (await admin.PostAsync("/products", form)).Content.ReadFromJsonAsync<ProductDetailResponse>();

        // One kept order (net 100) and one voided order (net 250) — only the kept one should count.
        var kept = await (await admin.PostAsJsonAsync("/sales-orders", new
        {
            memberId = (Guid?)null,
            lines = new[] { new { productId = product!.Id, quantity = 2 } },
        })).Content.ReadFromJsonAsync<SalesOrderResponse>();

        var voided = await (await admin.PostAsJsonAsync("/sales-orders", new
        {
            memberId = (Guid?)null,
            lines = new[] { new { productId = product.Id, quantity = 5 } },
        })).Content.ReadFromJsonAsync<SalesOrderResponse>();
        await admin.PostAsync($"/sales-orders/{voided!.Id}/void", null);

        var summary = await (await admin.GetAsync($"/reports/sales-summary?granularity=daily&from={today}&to={today}"))
            .Content.ReadFromJsonAsync<SalesSummaryResponse>();
        var todayEntry = summary!.Items.Should().ContainSingle(i => i.Period == today.ToString("yyyy-MM-dd")).Subject;
        todayEntry.TotalSales.Should().BeGreaterThanOrEqualTo(kept!.NetTotal, "the kept order's net total must be counted");

        var bestSellers = await (await admin.GetAsync($"/reports/best-sellers?from={today}&to={today}"))
            .Content.ReadFromJsonAsync<BestSellersResponse>();
        var entry = bestSellers!.Items.Should().ContainSingle(i => i.ProductId == product.Id).Subject;
        entry.QuantitySold.Should().Be(2, "the voided 5-unit order must not contribute to best-sellers (FR-033)");
    }

    [Fact]
    public async Task Stock_levels_reflects_current_quantity_on_hand()
    {
        var admin = await CreateAdminClientAsync();
        using var form = MultipartFormBuilder.ProductForm($"StockLevelItem-{Guid.NewGuid():N}", 10m, 33);
        var product = await (await admin.PostAsync("/products", form)).Content.ReadFromJsonAsync<ProductDetailResponse>();

        var stockLevels = await (await admin.GetAsync("/reports/stock-levels"))
            .Content.ReadFromJsonAsync<StockLevelsResponse>();

        stockLevels!.Items.Should().Contain(i => i.ProductId == product!.Id && i.QuantityOnHand == 33);
    }

    private sealed record ProductDetailResponse(Guid Id, string Name, string? ImageUrl, decimal Price, int QuantityOnHand, string? Barcode, int? LowStockThreshold, bool IsActive, bool LowStock);

    private sealed record SalesOrderResponse(Guid Id, Guid StaffId, Guid? MemberId, string Status, decimal NetTotal);

    private sealed record SalesSummaryItem(string Period, decimal TotalSales, int OrderCount);

    private sealed record SalesSummaryResponse(List<SalesSummaryItem> Items);

    private sealed record BestSellerItem(Guid ProductId, string ProductName, int QuantitySold, decimal TotalAmount);

    private sealed record BestSellersResponse(List<BestSellerItem> Items);

    private sealed record StockLevelItem(Guid ProductId, string ProductName, int QuantityOnHand, bool LowStock);

    private sealed record StockLevelsResponse(List<StockLevelItem> Items);
}
