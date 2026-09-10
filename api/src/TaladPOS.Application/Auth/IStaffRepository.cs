using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.Auth;

public interface IStaffRepository
{
    Task<Staff?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
}
