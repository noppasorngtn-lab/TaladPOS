using System.Net;
using System.Net.Http.Json;
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

    private sealed record MemberResponse(Guid Id, string Name, string PhoneNumber, decimal AccumulatedPurchaseTotal);

    private sealed record PricingPreviewResponse(decimal SubtotalAmount, decimal PromotionDiscountAmount, decimal MemberDiscountAmount, decimal NetTotal);
}
