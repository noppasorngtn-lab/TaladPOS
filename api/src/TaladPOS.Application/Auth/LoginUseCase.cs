using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.Auth;

public record LoginResult(bool Succeeded, JwtToken? Token = null, Staff? Staff = null);

/// <summary>Validates staff credentials and issues a JWT (FR-007, contracts/auth.md POST /auth/login).</summary>
public class LoginUseCase(IStaffRepository staffRepository, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
{
    public async Task<LoginResult> ExecuteAsync(string username, string password, CancellationToken cancellationToken)
    {
        var staff = await staffRepository.FindByUsernameAsync(username, cancellationToken);
        if (staff is null || !staff.IsActive || !passwordHasher.Verify(password, staff.PasswordHash))
        {
            return new LoginResult(Succeeded: false);
        }

        var token = jwtTokenService.IssueToken(staff);
        return new LoginResult(Succeeded: true, Token: token, Staff: staff);
    }
}
