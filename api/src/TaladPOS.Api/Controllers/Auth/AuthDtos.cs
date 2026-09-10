namespace TaladPOS.Api.Controllers.Auth;

public record LoginRequest(string Username, string Password);

public record StaffSummaryResponse(Guid Id, string Name, string Role);

public record LoginResponse(string Token, DateTimeOffset ExpiresAt, StaffSummaryResponse Staff);
