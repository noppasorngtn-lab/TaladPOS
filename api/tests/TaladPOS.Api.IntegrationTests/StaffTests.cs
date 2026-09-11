using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaladPOS.Api.Controllers.Staff;

namespace TaladPOS.Api.IntegrationTests;

public class StaffTests(TaladPosApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Create_then_login_as_the_new_staff_succeeds()
    {
        var admin = await CreateAdminClientAsync();
        var username = $"newstaff-{Guid.NewGuid():N}";

        var createResponse = await admin.PostAsJsonAsync("/staff", new
        {
            name = "New Staff",
            username,
            password = "password1",
            role = "Cashier",
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginResponse = await Factory.CreateClient().PostAsJsonAsync("/auth/login", new { username, password = "password1" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_with_duplicate_username_returns_422()
    {
        var admin = await CreateAdminClientAsync();
        var username = $"dupstaff-{Guid.NewGuid():N}";
        var body = new { name = "Dup", username, password = "password1", role = "Cashier" };

        (await admin.PostAsJsonAsync("/staff", body)).StatusCode.Should().Be(HttpStatusCode.Created);
        var second = await admin.PostAsJsonAsync("/staff", body);

        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_with_password_shorter_than_8_characters_returns_422()
    {
        var admin = await CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/staff", new
        {
            name = "Short Pw",
            username = $"shortpw-{Guid.NewGuid():N}",
            password = "short1",
            role = "Cashier",
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_with_username_containing_a_space_returns_422()
    {
        var admin = await CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/staff", new
        {
            name = "Bad Username",
            username = $"bad user-{Guid.NewGuid():N}",
            password = "password1",
            role = "Cashier",
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_with_invalid_role_returns_400()
    {
        var admin = await CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/staff", new
        {
            name = "Bad Role",
            username = $"badrole-{Guid.NewGuid():N}",
            password = "password1",
            role = "SuperAdmin",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Edit_role_then_next_auth_me_reflects_the_new_role()
    {
        var admin = await CreateAdminClientAsync();
        var username = $"rolechange-{Guid.NewGuid():N}";
        var created = await (await admin.PostAsJsonAsync("/staff", new
        {
            name = "Role Change",
            username,
            password = "password1",
            role = "Cashier",
        })).Content.ReadFromJsonAsync<StaffSummaryResponse>();

        (await admin.PutAsJsonAsync($"/staff/{created!.Id}", new { name = "Role Change", role = "Admin" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var staffLogin = await Factory.CreateClient().PostAsJsonAsync("/auth/login", new { username, password = "password1" });
        var loginBody = await staffLogin.Content.ReadFromJsonAsync<LoginResponseForTest>();
        loginBody!.Staff.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Deactivate_then_login_returns_401_and_activate_restores_login()
    {
        var admin = await CreateAdminClientAsync();
        var username = $"deactivate-{Guid.NewGuid():N}";
        var created = await (await admin.PostAsJsonAsync("/staff", new
        {
            name = "Deactivate Me",
            username,
            password = "password1",
            role = "Cashier",
        })).Content.ReadFromJsonAsync<StaffSummaryResponse>();

        (await admin.PostAsync($"/staff/{created!.Id}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var loginWhileDeactivated = await Factory.CreateClient().PostAsJsonAsync("/auth/login", new { username, password = "password1" });
        loginWhileDeactivated.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await admin.PostAsync($"/staff/{created.Id}/activate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var loginAfterReactivation = await Factory.CreateClient().PostAsJsonAsync("/auth/login", new { username, password = "password1" });
        loginAfterReactivation.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reset_password_then_login_with_new_password_succeeds_and_old_password_fails()
    {
        var admin = await CreateAdminClientAsync();
        var username = $"resetpw-{Guid.NewGuid():N}";
        var created = await (await admin.PostAsJsonAsync("/staff", new
        {
            name = "Reset Pw",
            username,
            password = "password1",
            role = "Cashier",
        })).Content.ReadFromJsonAsync<StaffSummaryResponse>();

        (await admin.PostAsJsonAsync($"/staff/{created!.Id}/reset-password", new { newPassword = "newpassword1" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var oldPasswordLogin = await Factory.CreateClient().PostAsJsonAsync("/auth/login", new { username, password = "password1" });
        oldPasswordLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newPasswordLogin = await Factory.CreateClient().PostAsJsonAsync("/auth/login", new { username, password = "newpassword1" });
        newPasswordLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deactivating_or_role_changing_the_sole_active_admin_returns_409()
    {
        var admin = await CreateAdminClientAsync();

        // Deterministic precondition regardless of what other tests in this shared-DB collection
        // may have left behind: deactivate every OTHER currently-active Admin so the seeded
        // "admin" account is guaranteed to be the sole active Admin before asserting the guard.
        var list = await (await admin.GetAsync("/staff?pageSize=200")).Content.ReadFromJsonAsync<StaffSearchResponse>();
        foreach (var other in list!.Items.Where(s => s.Role == "Admin" && s.IsActive && s.Id != Factory.AdminStaffId))
        {
            await admin.PostAsync($"/staff/{other.Id}/deactivate", null);
        }

        var deactivateSelf = await admin.PostAsync($"/staff/{Factory.AdminStaffId}/deactivate", null);
        deactivateSelf.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var roleChangeSelf = await admin.PutAsJsonAsync($"/staff/{Factory.AdminStaffId}", new { name = "System Administrator", role = "Cashier" });
        roleChangeSelf.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private sealed record LoginResponseForTest(string Token, StaffSummaryForTest Staff);

    private sealed record StaffSummaryForTest(Guid Id, string Name, string Role);
}
