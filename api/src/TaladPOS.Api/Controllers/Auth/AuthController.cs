using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Auth;

namespace TaladPOS.Api.Controllers.Auth;

[ApiController]
[Route("auth")]
public class AuthController(LoginUseCase loginUseCase) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await loginUseCase.ExecuteAsync(request.Username, request.Password, cancellationToken);
        if (!result.Succeeded)
        {
            return Unauthorized(new { error = new { code = "invalid_credentials", message = "Invalid username or password." } });
        }

        var staff = result.Staff!;
        var token = result.Token!;
        return Ok(new LoginResponse(
            token.Value,
            token.ExpiresAt,
            new StaffSummaryResponse(staff.Id, staff.Name, staff.Role.ToString())));
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout() =>
        // Stateless JWT — the client discards the token; this endpoint exists for symmetry
        // and future token-revocation support (contracts/auth.md).
        NoContent();

    [HttpGet("me")]
    [Authorize]
    public ActionResult<StaffSummaryResponse> Me()
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var name = User.FindFirstValue(ClaimTypes.Name)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        return Ok(new StaffSummaryResponse(id, name, role));
    }
}
