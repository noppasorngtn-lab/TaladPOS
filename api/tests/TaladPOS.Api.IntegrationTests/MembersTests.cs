using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaladPOS.Api.IntegrationTests.Helpers;

namespace TaladPOS.Api.IntegrationTests;

public class MembersTests(TaladPosApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Signup_creates_member_with_zero_accumulated_total()
    {
        var admin = await CreateAdminClientAsync();
        var phone = UniquePhone();

        var response = await admin.PostAsJsonAsync("/members", new { phoneNumber = phone, name = "New Member" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var member = await response.Content.ReadFromJsonAsync<MemberResponse>();
        member!.AccumulatedPurchaseTotal.Should().Be(0m);
    }

    [Fact]
    public async Task Signup_with_duplicate_phone_returns_422()
    {
        var admin = await CreateAdminClientAsync();
        var phone = UniquePhone();
        await admin.PostAsJsonAsync("/members", new { phoneNumber = phone, name = "First" });

        var response = await admin.PostAsJsonAsync("/members", new { phoneNumber = phone, name = "Second" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Lookup_by_unknown_phone_returns_404()
    {
        var admin = await CreateAdminClientAsync();

        var response = await admin.GetAsync($"/members?phone={UniquePhone()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Checkout_linked_to_member_credits_and_void_reverses_accumulated_total()
    {
        var admin = await CreateAdminClientAsync();
        var phone = UniquePhone();
        var member = await (await admin.PostAsJsonAsync("/members", new { phoneNumber = phone, name = "Loyal Customer" }))
            .Content.ReadFromJsonAsync<MemberResponse>();

        using var form = MultipartFormBuilder.ProductForm($"MemberSaleItem-{Guid.NewGuid():N}", 40m, 10);
        var product = await (await admin.PostAsync("/products", form)).Content.ReadFromJsonAsync<ProductDetailResponse>();

        var order = await (await admin.PostAsJsonAsync("/sales-orders", new
        {
            memberId = member!.Id,
            lines = new[] { new { productId = product!.Id, quantity = 2 } },
        })).Content.ReadFromJsonAsync<SalesOrderResponse>();

        var afterSale = await (await admin.GetAsync($"/members/{member.Id}")).Content.ReadFromJsonAsync<MemberResponse>();
        afterSale!.AccumulatedPurchaseTotal.Should().Be(order!.NetTotal,
            "accumulated total must increase by the order's net amount, independent of whatever promotions happen to be active (spec.md US3 Independent Test)");

        await admin.PostAsync($"/sales-orders/{order!.Id}/void", null);

        var afterVoid = await (await admin.GetAsync($"/members/{member.Id}")).Content.ReadFromJsonAsync<MemberResponse>();
        afterVoid!.AccumulatedPurchaseTotal.Should().Be(0m);
    }

    private static string UniquePhone() => $"08{Random.Shared.Next(10000000, 99999999)}";

    private sealed record MemberResponse(Guid Id, string Name, string PhoneNumber, decimal AccumulatedPurchaseTotal);

    private sealed record ProductDetailResponse(Guid Id, string Name, string? ImageUrl, decimal Price, int QuantityOnHand, string? Barcode, int? LowStockThreshold, bool IsActive, bool LowStock);

    private sealed record SalesOrderResponse(Guid Id, Guid StaffId, Guid? MemberId, string Status, decimal NetTotal);
}
