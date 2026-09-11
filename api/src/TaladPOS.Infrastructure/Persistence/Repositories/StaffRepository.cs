using Microsoft.EntityFrameworkCore;
using TaladPOS.Application.Auth;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Infrastructure.Persistence.Repositories;

public class StaffRepository(TaladPOSDbContext dbContext) : IStaffRepository
{
    public Task<Staff?> FindByUsernameAsync(string username, CancellationToken cancellationToken) =>
        dbContext.Staff.SingleOrDefaultAsync(s => s.Username == username, cancellationToken);

    public Task<Staff?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Staff.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Staff> Items, int Total)> SearchAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Staff.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Name.Contains(search) || s.Username.Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<bool> UsernameExistsAsync(string username, Guid? excludeStaffId, CancellationToken cancellationToken) =>
        dbContext.Staff.AnyAsync(
            s => s.Username == username && (excludeStaffId == null || s.Id != excludeStaffId),
            cancellationToken);

    public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken) =>
        dbContext.Staff.CountAsync(s => s.IsActive && s.Role == StaffRole.Admin, cancellationToken);

    public Task AddAsync(Staff staff, CancellationToken cancellationToken)
    {
        dbContext.Staff.Add(staff);
        return Task.CompletedTask;
    }

    public Task AddAuditLogAsync(StaffAuditLog auditLog, CancellationToken cancellationToken)
    {
        dbContext.StaffAuditLogs.Add(auditLog);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
