using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaladPOS.Api.IntegrationTests.Helpers;

namespace TaladPOS.Api.IntegrationTests;

public class ProductsTests(TaladPosApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Create_then_GetById_returns_the_new_product()
    {
        var admin = await CreateAdminClientAsync();

        using var form = MultipartFormBuilder.ProductForm("Apple", 15m, 10, barcode: "APPLE-001", lowStockThreshold: 3);
        var createResponse = await admin.PostAsync("/products", form);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDetailResponse>();

        var getResponse = await admin.GetAsync($"/products/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductDetailResponse>();
        fetched!.Name.Should().Be("Apple");
        fetched.Price.Should().Be(15m);
        fetched.QuantityOnHand.Should().Be(10);
    }

    [Fact]
    public async Task Create_with_price_zero_returns_422()
    {
        var admin = await CreateAdminClientAsync();

        using var form = MultipartFormBuilder.ProductForm("Bad Price", 0m, 10);
        var response = await admin.PostAsync("/products", form);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_with_duplicate_barcode_returns_422()
    {
        var admin = await CreateAdminClientAsync();

        using var first = MultipartFormBuilder.ProductForm("Banana", 5m, 10, barcode: "DUP-BARCODE");
        (await admin.PostAsync("/products", first)).StatusCode.Should().Be(HttpStatusCode.Created);

        using var second = MultipartFormBuilder.ProductForm("Banana 2", 5m, 10, barcode: "DUP-BARCODE");
        var response = await admin.PostAsync("/products", second);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Edit_price_does_not_change_id_and_is_reflected_on_search()
    {
        var admin = await CreateAdminClientAsync();

        using var createForm = MultipartFormBuilder.ProductForm("Orange", 20m, 10);
        var created = await (await admin.PostAsync("/products", createForm)).Content.ReadFromJsonAsync<ProductDetailResponse>();

        using var editForm = MultipartFormBuilder.ProductForm("Orange", 25m, 10);
        var editResponse = await admin.PutAsync($"/products/{created!.Id}", editForm);
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var edited = await editResponse.Content.ReadFromJsonAsync<ProductDetailResponse>();
        edited!.Id.Should().Be(created.Id);
        edited.Price.Should().Be(25m);

        var search = await (await admin.GetAsync("/products?search=Orange"))
            .Content.ReadFromJsonAsync<ProductSearchResponse>();
        search!.Items.Should().ContainSingle(p => p.Id == created.Id && p.Price == 25m);
    }

    [Fact]
    public async Task Product_at_or_below_threshold_appears_in_low_stock()
    {
        var admin = await CreateAdminClientAsync();

        using var form = MultipartFormBuilder.ProductForm("LowStockItem", 10m, 2, lowStockThreshold: 5);
        var created = await (await admin.PostAsync("/products", form)).Content.ReadFromJsonAsync<ProductDetailResponse>();

        var lowStock = await (await admin.GetAsync("/products/low-stock"))
            .Content.ReadFromJsonAsync<ProductSearchResponse>();
        lowStock!.Items.Should().Contain(p => p.Id == created!.Id);
    }

    [Fact]
    public async Task Delete_soft_deletes_and_product_no_longer_appears_in_default_search()
    {
        var admin = await CreateAdminClientAsync();

        using var form = MultipartFormBuilder.ProductForm("ToDelete", 10m, 10);
        var created = await (await admin.PostAsync("/products", form)).Content.ReadFromJsonAsync<ProductDetailResponse>();

        var deleteResponse = await admin.DeleteAsync($"/products/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var search = await (await admin.GetAsync("/products?search=ToDelete"))
            .Content.ReadFromJsonAsync<ProductSearchResponse>();
        search!.Items.Should().NotContain(p => p.Id == created.Id);
    }

    [Fact]
    public async Task GetById_for_unknown_id_returns_404()
    {
        var admin = await CreateAdminClientAsync();

        var response = await admin.GetAsync($"/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record ProductDetailResponse(
        Guid Id, string Name, string? ImageUrl, decimal Price, int QuantityOnHand,
        string? Barcode, int? LowStockThreshold, bool IsActive, bool LowStock);

    private sealed record ProductSummaryResponse(Guid Id, string Name, string? ImageUrl, decimal Price, int QuantityOnHand, string? Barcode, bool LowStock);

    private sealed record ProductSearchResponse(List<ProductSummaryResponse> Items, int Total);
}
