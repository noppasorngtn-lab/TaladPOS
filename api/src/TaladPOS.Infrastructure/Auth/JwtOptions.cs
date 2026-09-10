namespace TaladPOS.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int ExpiryMinutes { get; set; } = 60 * 8;
}
