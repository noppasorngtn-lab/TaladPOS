using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.Auth;

public interface IJwtTokenService
{
    JwtToken IssueToken(Staff staff);
}

/// <summary>The signed token plus the expiry the API promised the client (contracts/auth.md).</summary>
public record JwtToken(string Value, DateTimeOffset ExpiresAt);
