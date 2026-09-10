using TaladPOS.Domain.Entities;

namespace TaladPOS.Infrastructure.Persistence.Seed;

/// <summary>
/// Bootstraps the very first login (quickstart.md prerequisites) with one Admin account.
/// Username: admin / Password: Admin@12345 — change this immediately after first login;
/// it exists only so a brand-new database has *someone* who can create real staff accounts.
/// The hash below is PBKDF2-HMAC-SHA256, 100,000 iterations (matches Infrastructure.Auth.PasswordHasher).
/// </summary>
public static class AdminSeed
{
    public static readonly Guid AdminStaffId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static object GetSeedData() => new
    {
        Id = AdminStaffId,
        Name = "System Administrator",
        Username = "admin",
        PasswordHash = "100000.qJbT6qmMZ7LM4WSzJQ0kTQ==./j1yVCxzraMfhxXnvOplW7XTVkMGDlqwKpprbahxAgg=",
        Role = StaffRole.Admin,
        IsActive = true,
    };
}
