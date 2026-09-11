using System.Text.Json.Serialization;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Api.Controllers.Staff;

public record StaffSummaryResponse(Guid Id, string Name, string Username, string Role, bool IsActive);

public record StaffSearchResponse(List<StaffSummaryResponse> Items, int Total);

// role is bound as the StaffRole enum (not string) so a value outside "Cashier"/"Admin" fails
// model binding and returns 400 automatically (contracts/staff.md) — JsonStringEnumConverter
// keeps the wire format the documented string, not the default numeric enum encoding.
public record CreateStaffRequest(
    string Name,
    string Username,
    string Password,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] StaffRole Role);

public record EditStaffRequest(
    string Name,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] StaffRole Role);

public record ResetPasswordRequest(string NewPassword);
