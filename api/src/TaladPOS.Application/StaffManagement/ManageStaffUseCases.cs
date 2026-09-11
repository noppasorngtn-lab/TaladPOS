using System.Text.RegularExpressions;
using TaladPOS.Application.Auth;
using TaladPOS.Domain.Entities;
using TaladPOS.Domain.Exceptions;

namespace TaladPOS.Application.StaffManagement;

public record CreateStaffRequest(string Name, string Username, string Password, StaffRole Role);

public record EditStaffRequest(string Name, StaffRole Role);

/// <summary>Staff account create/edit/activate/deactivate/reset-password for the permission
/// management screen (FR-008, FR-010–FR-012, FR-016), plus append-only audit logging (FR-014).</summary>
public partial class ManageStaffUseCases(IStaffRepository staffRepository, IPasswordHasher passwordHasher)
{
    private const int MinPasswordLength = 8;

    [GeneratedRegex(@"^[A-Za-z0-9_.-]+$")]
    private static partial Regex UsernamePattern();

    public async Task<Staff> CreateAsync(CreateStaffRequest request, Guid performedByStaffId, CancellationToken cancellationToken)
    {
        EnsureUsernameIsWellFormed(request.Username);
        EnsurePasswordIsLongEnough(request.Password);
        await EnsureUsernameIsAvailableAsync(request.Username, excludeStaffId: null, cancellationToken);

        var passwordHash = passwordHasher.Hash(request.Password);
        var staff = new Staff(request.Name, request.Username, passwordHash, request.Role);

        await staffRepository.AddAsync(staff, cancellationToken);
        await AddAuditLogAsync(staff.Id, performedByStaffId, StaffAuditAction.Created, cancellationToken);
        await staffRepository.SaveChangesAsync(cancellationToken);
        return staff;
    }

    public async Task<Staff> EditAsync(Guid id, EditStaffRequest request, Guid performedByStaffId, CancellationToken cancellationToken)
    {
        var staff = await staffRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Staff {id} was not found.");

        var roleChanged = staff.Role != request.Role;
        if (roleChanged && staff.Role == StaffRole.Admin && request.Role != StaffRole.Admin)
        {
            await EnsureNotLastActiveAdminAsync(staff, cancellationToken);
        }

        staff.Rename(request.Name);
        staff.ChangeRole(request.Role);

        var action = roleChanged ? StaffAuditAction.RoleChanged : StaffAuditAction.Edited;
        await AddAuditLogAsync(staff.Id, performedByStaffId, action, cancellationToken);
        await staffRepository.SaveChangesAsync(cancellationToken);
        return staff;
    }

    public async Task DeactivateAsync(Guid id, Guid performedByStaffId, CancellationToken cancellationToken)
    {
        var staff = await staffRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Staff {id} was not found.");

        if (staff.Role == StaffRole.Admin)
        {
            await EnsureNotLastActiveAdminAsync(staff, cancellationToken);
        }

        staff.Deactivate();
        await AddAuditLogAsync(staff.Id, performedByStaffId, StaffAuditAction.Deactivated, cancellationToken);
        await staffRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateAsync(Guid id, Guid performedByStaffId, CancellationToken cancellationToken)
    {
        var staff = await staffRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Staff {id} was not found.");

        staff.Activate();
        await AddAuditLogAsync(staff.Id, performedByStaffId, StaffAuditAction.Activated, cancellationToken);
        await staffRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(Guid id, string newPassword, Guid performedByStaffId, CancellationToken cancellationToken)
    {
        var staff = await staffRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Staff {id} was not found.");

        EnsurePasswordIsLongEnough(newPassword);

        staff.SetPasswordHash(passwordHasher.Hash(newPassword));
        await AddAuditLogAsync(staff.Id, performedByStaffId, StaffAuditAction.PasswordReset, cancellationToken);
        await staffRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNotLastActiveAdminAsync(Staff staff, CancellationToken cancellationToken)
    {
        if (!staff.IsActive)
        {
            return;
        }

        var activeAdmins = await staffRepository.CountActiveAdminsAsync(cancellationToken);
        if (activeAdmins <= 1)
        {
            throw new LastActiveAdminException(staff.Id);
        }
    }

    private async Task EnsureUsernameIsAvailableAsync(string username, Guid? excludeStaffId, CancellationToken cancellationToken)
    {
        var isTaken = await staffRepository.UsernameExistsAsync(username, excludeStaffId, cancellationToken);
        if (isTaken)
        {
            throw new ArgumentException($"Username '{username}' is already taken.", nameof(username));
        }
    }

    private static void EnsureUsernameIsWellFormed(string username)
    {
        if (string.IsNullOrWhiteSpace(username) || !UsernamePattern().IsMatch(username))
        {
            throw new ArgumentException(
                "Username must contain only letters, digits, '_', '.', or '-', with no whitespace.",
                nameof(username));
        }
    }

    private static void EnsurePasswordIsLongEnough(string password)
    {
        if (password.Length < MinPasswordLength)
        {
            throw new ArgumentException($"Password must be at least {MinPasswordLength} characters long.", nameof(password));
        }
    }

    private Task AddAuditLogAsync(Guid staffId, Guid performedByStaffId, StaffAuditAction action, CancellationToken cancellationToken) =>
        staffRepository.AddAuditLogAsync(new StaffAuditLog(staffId, performedByStaffId, action), cancellationToken);
}
