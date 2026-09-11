namespace TaladPOS.Domain.Exceptions;

/// <summary>Raised when an operation would leave the system with zero active Admin accounts
/// (FR-012) — deactivating or role-changing the last active Admin.</summary>
public class LastActiveAdminException(Guid staffId)
    : DomainConflictException(
        "last_active_admin",
        $"Staff {staffId} is the last active Admin — the system must always have at least one active Admin account.")
{
    public Guid StaffId { get; } = staffId;
}
