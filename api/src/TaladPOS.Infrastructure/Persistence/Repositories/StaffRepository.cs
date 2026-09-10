using Microsoft.EntityFrameworkCore;
using TaladPOS.Application.Auth;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Infrastructure.Persistence.Repositories;

public class StaffRepository(TaladPOSDbContext dbContext) : IStaffRepository
{
    public Task<Staff?> FindByUsernameAsync(string username, CancellationToken cancellationToken) =>
        dbContext.Staff.SingleOrDefaultAsync(s => s.Username == username, cancellationToken);
}
