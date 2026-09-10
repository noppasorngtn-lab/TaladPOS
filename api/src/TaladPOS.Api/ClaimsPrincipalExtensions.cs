using System.Security.Claims;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Api;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetStaffId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!);

    public static bool IsInRole(this ClaimsPrincipal user, StaffRole role) => user.IsInRole(role.ToString());
}
