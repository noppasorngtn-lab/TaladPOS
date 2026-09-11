using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.Auth;

public interface IStaffRepository
{
    Task<Staff?> FindByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<Staff?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Matches by name-contains or username-contains (contracts/staff.md GET /staff).</summary>
    Task<(IReadOnlyList<Staff> Items, int Total)> SearchAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Backs the FR-009 username-uniqueness rule; excludeStaffId lets an edit keep its
    /// own username without flagging itself as a conflict.</summary>
    Task<bool> UsernameExistsAsync(string username, Guid? excludeStaffId, CancellationToken cancellationToken);

    /// <summary>Backs the FR-012 last-active-Admin guard.</summary>
    Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken);

    Task AddAsync(Staff staff, CancellationToken cancellationToken);

    Task AddAuditLogAsync(StaffAuditLog auditLog, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
