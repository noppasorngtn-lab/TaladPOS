using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TaladPOS.Api.IntegrationTests.Helpers;

namespace TaladPOS.Api.IntegrationTests;

/// <summary>T064: Admin-only endpoints must reject a Cashier-role JWT with 403, and an
/// expired/invalid/missing JWT must be rejected with 401, regardless of the role it claims.</summary>
public class SecurityTests(TaladPosApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Admin_only_product_create_rejects_cashier_role_with_403()
    {
        var cashier = await CreateCashierClientAsync();

        using var form = MultipartFormBuilder.ProductForm($"CashierAttempt-{Guid.NewGuid():N}", 10m, 5);
        var response = await cashier.PostAsync("/products", form);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_only_low_stock_endpoint_rejects_cashier_role_with_403()
    {
        var cashier = await CreateCashierClientAsync();

        var response = await cashier.GetAsync("/products/low-stock");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_only_reports_endpoint_rejects_cashier_role_with_403()
    {
        var cashier = await CreateCashierClientAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var response = await cashier.GetAsync($"/reports/stock-levels?from={today}&to={today}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cashier_can_reach_non_admin_endpoints()
    {
        var cashier = await CreateCashierClientAsync();

        var response = await cashier.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "search is any-authenticated-staff per contracts/products.md");
    }

    [Fact]
    public async Task Missing_token_returns_401()
    {
        var anonymous = Factory.CreateClient();

        var response = await anonymous.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Garbage_token_returns_401()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-jwt");

        var response = await client.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invalid_credentials_returns_401_not_a_token()
    {
        var anonymous = Factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/auth/login", new { username = "admin", password = "WrongPassword!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
