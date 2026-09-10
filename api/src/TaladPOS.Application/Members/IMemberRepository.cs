using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.Members;

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Member lookup by phone during checkout (FR-017).</summary>
    Task<Member?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken);

    /// <summary>Backs <see cref="Member.EnsurePhoneNumberIsAvailable"/> (FR-016).</summary>
    Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken cancellationToken);

    Task AddAsync(Member member, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
