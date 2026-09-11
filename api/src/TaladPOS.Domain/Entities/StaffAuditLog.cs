namespace TaladPOS.Domain.Entities;

public enum StaffAuditAction
{
    Created,
    Edited,
    RoleChanged,
    Activated,
    Deactivated,
    PasswordReset,
}

/// <summary>Append-only record of who changed a Staff account, and when (FR-014). No API/UI
/// surfaces this in this version — stored for future auditability.</summary>
public class StaffAuditLog
{
    public Guid Id { get; private set; }
    public Guid StaffId { get; private set; }
    public Guid PerformedByStaffId { get; private set; }
    public StaffAuditAction Action { get; private set; }
    public DateTime Timestamp { get; private set; }

    private StaffAuditLog()
    {
    }

    public StaffAuditLog(Guid staffId, Guid performedByStaffId, StaffAuditAction action)
    {
        Id = Guid.NewGuid();
        StaffId = staffId;
        PerformedByStaffId = performedByStaffId;
        Action = action;
        Timestamp = DateTime.UtcNow;
    }
}
