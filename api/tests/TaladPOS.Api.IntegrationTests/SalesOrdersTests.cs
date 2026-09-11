using System.Net;
using System.Net.Http.Json;
using ClosedXML.Excel;
using FluentAssertions;
using TaladPOS.Api.IntegrationTests.Helpers;

namespace TaladPOS.Api.IntegrationTests;

public class SalesOrdersTests(TaladPosApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Checkout_decrements_stock_and_records_staff_and_net_total()
    {
        var admin = await CreateAdminClientAsync();
        var productId = await CreateProductAsync(admin, "Mango", price: 28m, quantityOnHand: 20);

        var checkoutResponse = await admin.PostAsJsonAsync("/sales-orders", new
        {
            memberId = (Guid?)null,
            lines = new[] { new { productId, quantity = 3 } },
        });

        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await checkoutResponse.Content.ReadFromJsonAsync<SalesOrderResponse>();
        order!.Status.Should().Be("Completed");
        order.NetTotal.Should().Be(84m);
        order.StaffId.Should().Be(Factory.AdminStaffId);

        var product = await GetProductAsync(admin, productId);
        product.QuantityOnHand.Should().Be(17);
    }

    [Fact]
    public async Task Checkout_with_quantity_exceeding_stock_returns_409_and_creates_no_order()
    {
        var admin = await CreateAdminClientAsync();
        var productId = await CreateProductAsync(admin, "ScarceItem", price: 10m, quantityOnHand: 2);

        var response = await admin.PostAsJsonAsync("/sales-orders", new
        {
            memberId = (Guid?)null,
            lines = new[] { new { productId, quantity = 5 } },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var product = await GetProductAsync(admin, productId);
        product.QuantityOnHand.Should().Be(2, "a rejected checkout must not partially decrement stock");
    }

    [Fact]
    public async Task Void_restores_stock_and_voiding_twice_returns_409()
    {
        var admin = await CreateAdminClientAsync();
        var productId = await CreateProductAsync(admin, "Voidable", price: 10m, quantityOnHand: 10);

        var order = await (await admin.PostAsJsonAsync("/sales-orders", new
        {
            memberId = (Guid?)null,
            lines = new[] { new { productId, quantity = 4 } },
        })).Content.ReadFromJsonAsync<SalesOrderResponse>();

        var voidResponse = await admin.PostAsync($"/sales-orders/{order!.Id}/void", null);
        voidResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var product = await GetProductAsync(admin, productId);
        product.QuantityOnHand.Should().Be(10, "voiding must restore the decremented stock");

        var secondVoid = await admin.PostAsync($"/sales-orders/{order.Id}/void", null);
        secondVoid.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Pricing_preview_applies_promotion_then_member_discount_sequentially()
    {
        var admin = await CreateAdminClientAsync();
        var productId = await CreateProductAsync(admin, "DiscountedItem", price: 100m, quantityOnHand: 50);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await admin.PostAsJsonAsync("/promotions", new
        {
            scope = "PerProduct",
            productId,
            discountPercent = 10m,
            startDate = today,
            endDate = today,
        });
        await admin.PostAsJsonAsync("/promotions", new
        {
            scope = "MemberDiscount",
            productId = (Guid?)null,
            discountPercent = 5m,
            startDate = today,
            endDate = today,
        });

        var memberResponse = await admin.PostAsJsonAsync("/members", new
        {
            phoneNumber = $"09{Random.Shared.Next(10000000, 99999999)}",
            name = "Discount Tester",
        });
        var member = await memberResponse.Content.ReadFromJsonAsync<MemberResponse>();

        // subtotal 200 -> promo 10% off -> 180 -> member 5% off remainder -> 171
        var preview = await (await admin.GetAsync(
                $"/sales-orders/pricing-preview?productId={productId}&quantity=2&memberId={member!.Id}"))
            .Content.ReadFromJsonAsync<PricingPreviewResponse>();

        preview!.SubtotalAmount.Should().Be(200m);
        preview.PromotionDiscountAmount.Should().Be(20m);
        preview.MemberDiscountAmount.Should().Be(9m);
        preview.NetTotal.Should().Be(171m);
    }

    [Fact]
    public async Task Future_dated_promotion_is_not_applied_today()
    {
        var admin = await CreateAdminClientAsync();
        var productId = await CreateProductAsync(admin, "FuturePromoItem", price: 50m, quantityOnHand: 10);
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

        await admin.PostAsJsonAsync("/promotions", new
        {
            scope = "PerProduct",
            productId,
            discountPercent = 50m,
            startDate = future,
            endDate = future.AddDays(5),
        });

        var preview = await (await admin.GetAsync($"/sales-orders/pricing-preview?productId={productId}&quantity=1"))
            .Content.ReadFromJsonAsync<PricingPreviewResponse>();

        preview!.PromotionDiscountAmount.Should().Be(0m);
        preview.NetTotal.Should().Be(50m);
    }

    // Feature 002-export-reports-sales-history, User Story 2 (FR-002–FR-005, FR-007–FR-010).
    [Fact]
    public async Task Export_returns_every_matching_order_not_just_one_page()
    {
        var admin = await CreateAdminClientAsync();
        var productId = await CreateProductAsync(admin, "ExportPageItem", price: 10m, quantityOnHand: 100);
        var member = await SignUpMemberAsync(admin);

        const int totalOrders = 25; // more than GET /sales-orders' default pageSize of 20
        for (var i = 0; i < totalOrders; i++)
        {
            var response = await admin.PostAsJsonAsync("/sales-orders", new
            {
                memberId = member,
                lines = new[] { new { productId, quantity = 1 } },
            });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var paged = await (await admin.GetAsync($"/sales-orders?memberId={member}"))
            .Content.ReadFromJsonAsync<SalesOrderSearchResponse>();
        paged!.Total.Should().Be(totalOrders);
        paged.Items.Count.Should().BeLessThan(totalOrders, "the on-screen search endpoint is paginated");

        var exportResponse = await admin.GetAsync($"/sales-orders/export?memberId={member}");
        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await ReadExportRowsAsync(exportResponse);
        rows.Should().HaveCount(totalOrders, "FR-004: export must include every matching order, not just one page");
    }

    [Fact]
    public async Task Export_includes_staff_and_member_names_and_blanks_member_when_unlinked()
    {
        var admin = await CreateAdminClientAsync();
        var productId = await CreateProductAsync(admin, "ExportNameItem", price: 20m, quantityOnHand: 10);
        var (member, memberName) = await SignUpMemberWithNameAsync(admin);

        var linkedOrder = await (await admin.PostAsJsonAsync("/sales-orders", new
        {
            memberId = member,
            lines = new[] { new { productId, quantity = 1 } },
        })).Content.ReadFromJsonAsync<SalesOrderResponse>();

        var unlinkedOrder = await (await admin.PostAsJsonAsync("/sales-orders", new
        {
            memberId = (Guid?)null,
            lines = new[] { new { productId, quantity = 1 } },
        })).Content.ReadFromJsonAsync<SalesOrderResponse>();

        var exportResponse = await admin.GetAsync($"/sales-orders/export?staffId={Factory.AdminStaffId}");
        var rows = await ReadExportRowsAsync(exportResponse);

        var linkedRow = rows.Should().ContainSingle(r => r.NetTotal == linkedOrder!.NetTotal && r.MemberName == memberName).Subject;
        linkedRow.StaffName.Should().NotBeNullOrWhiteSpace();

        var unlinkedRow = rows.Should().ContainSingle(r => r.NetTotal == unlinkedOrder!.NetTotal && string.IsNullOrEmpty(r.MemberName)).Subject;
        unlinkedRow.StaffName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Export_with_status_filter_returns_only_matching_status()
    {
        var admin = await CreateAdminClientAsync();
        var productId = await CreateProductAsync(admin, "ExportStatusItem", price: 30m, quantityOnHand: 10);
        var member = await SignUpMemberAsync(admin);

        var toVoid = await (await admin.PostAsJsonAsync("/sales-orders", new
        {
            memberId = member,
            lines = new[] { new { productId, quantity = 1 } },
        })).Content.ReadFromJsonAsync<SalesOrderResponse>();
        await admin.PostAsync($"/sales-orders/{toVoid!.Id}/void", null);

        await admin.PostAsJsonAsync("/sales-orders", new
        {
            memberId = member,
            lines = new[] { new { productId, quantity = 1 } },
        });

        var voidedOnlyResponse = await admin.GetAsync($"/sales-orders/export?memberId={member}&status=Voided");
        var voidedRows = await ReadExportRowsAsync(voidedOnlyResponse);
        voidedRows.Should().ContainSingle();
        voidedRows[0].Status.Should().Be("Voided");
    }

    [Fact]
    public async Task Export_with_no_matches_returns_header_only_workbook()
    {
        var admin = await CreateAdminClientAsync();
        var memberWithNoOrders = await SignUpMemberAsync(admin);

        var response = await admin.GetAsync($"/sales-orders/export?memberId={memberWithNoOrders}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await ReadExportRowsAsync(response);
        rows.Should().BeEmpty("FR-009: no matches still downloads successfully, just with no data rows");
    }

    [Fact]
    public async Task Export_rejects_cashier_role_with_403()
    {
        var cashier = await CreateCashierClientAsync();

        var response = await cashier.GetAsync("/sales-orders/export");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<Guid> SignUpMemberAsync(HttpClient admin) => (await SignUpMemberWithNameAsync(admin)).Id;

    private static async Task<(Guid Id, string Name)> SignUpMemberWithNameAsync(HttpClient admin)
    {
        var uniqueName = $"ExportMember-{Guid.NewGuid():N}";
        var response = await admin.PostAsJsonAsync("/members", new
        {
            phoneNumber = $"08{Random.Shared.Next(10000000, 99999999)}",
            name = uniqueName,
        });
        var member = await response.Content.ReadFromJsonAsync<MemberResponse>();
        return (member!.Id, uniqueName);
    }

    private static async Task<List<ExportRow>> ReadExportRowsAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType!.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        var rows = new List<ExportRow>();
        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            rows.Add(new ExportRow(
                row.Cell(2).GetString(),
                row.Cell(3).GetString(),
                decimal.Parse(row.Cell(4).GetString()),
                row.Cell(5).GetString()));
        }

        return rows;
    }

    private sealed record ExportRow(string StaffName, string MemberName, decimal NetTotal, string Status);

    private static async Task<Guid> CreateProductAsync(HttpClient admin, string name, decimal price, int quantityOnHand)
    {
        using var form = MultipartFormBuilder.ProductForm($"{name}-{Guid.NewGuid():N}", price, quantityOnHand);
        var response = await admin.PostAsync("/products", form);
        var created = await response.Content.ReadFromJsonAsync<ProductDetailResponse>();
        return created!.Id;
    }

    private static async Task<ProductDetailResponse> GetProductAsync(HttpClient admin, Guid id) =>
        (await (await admin.GetAsync($"/products/{id}")).Content.ReadFromJsonAsync<ProductDetailResponse>())!;

    private sealed record ProductDetailResponse(
        Guid Id, string Name, string? ImageUrl, decimal Price, int QuantityOnHand,
        string? Barcode, int? LowStockThreshold, bool IsActive, bool LowStock);

    private sealed record SalesOrderResponse(Guid Id, Guid StaffId, Guid? MemberId, string Status, decimal NetTotal);

    private sealed record SalesOrderSummaryResponse(Guid Id, DateTimeOffset CreatedAt, Guid StaffId, Guid? MemberId, string Status, decimal NetTotal);

    private sealed record SalesOrderSearchResponse(List<SalesOrderSummaryResponse> Items, int Total);

    private sealed record MemberResponse(Guid Id, string Name, string PhoneNumber, decimal AccumulatedPurchaseTotal);

    private sealed record PricingPreviewResponse(decimal SubtotalAmount, decimal PromotionDiscountAmount, decimal MemberDiscountAmount, decimal NetTotal);
}
