using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TaladPOS.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public abstract class ApiTestBase(TaladPosApiFactory factory)
{
    protected TaladPosApiFactory Factory { get; } = factory;

    protected async Task<HttpClient> CreateAuthenticatedClientAsync(string username, string password)
    {
        var anonymous = Factory.CreateClient();
        var response = await anonymous.PostAsJsonAsync("/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    protected Task<HttpClient> CreateAdminClientAsync() =>
        CreateAuthenticatedClientAsync("admin", "Admin@12345");

    protected Task<HttpClient> CreateCashierClientAsync() =>
        CreateAuthenticatedClientAsync(TaladPosApiFactory.CashierUsername, TaladPosApiFactory.CashierPassword);

    private sealed record LoginResponse(string Token);
}
