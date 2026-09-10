using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaladPOS.Application.Auth;
using TaladPOS.Domain.Entities;
using TaladPOS.Infrastructure.Persistence;
using TaladPOS.Infrastructure.Persistence.Seed;

namespace TaladPOS.Api.IntegrationTests;

/// <summary>
/// Boots the real API pipeline (auth, CORS, error middleware, controllers) against a dedicated
/// PostgreSQL database (`taladpos_test`), separate from the dev database, so contract tests
/// exercise the same code path a real client hits (research.md item 5's rationale extended to
/// tests: the Domain is the source of truth, so integration tests should go through it).
/// </summary>
public sealed class TaladPosApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string CashierUsername = "test-cashier";
    public const string CashierPassword = "Cashier@12345";

    public static Guid CashierStaffId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Host=localhost;Port=5433;Database=taladpos_test;Username=postgres;Password=postgres");
    }

    Task IAsyncLifetime.InitializeAsync() => ResetDatabaseAsync();

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    /// <summary>Resets the schema to a clean, deterministic state and seeds one Cashier account
    /// (the Admin account already comes from the InitialCreate migration's HasData seed).</summary>
    private async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaladPOSDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var cashier = new Staff("Test Cashier", CashierUsername, hasher.Hash(CashierPassword), StaffRole.Cashier);
        db.Set<Staff>().Add(cashier);
        await db.SaveChangesAsync();
        CashierStaffId = cashier.Id;
    }

    public Guid AdminStaffId => AdminSeed.AdminStaffId;
}
