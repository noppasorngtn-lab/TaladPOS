using Microsoft.EntityFrameworkCore;
using TaladPOS.Application.Members;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Infrastructure.Persistence.Repositories;

public class MemberRepository(TaladPOSDbContext dbContext) : IMemberRepository
{
    public Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Members.SingleOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<Member?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken) =>
        dbContext.Members.SingleOrDefaultAsync(m => m.PhoneNumber == phoneNumber, cancellationToken);

    public Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken cancellationToken) =>
        dbContext.Members.AnyAsync(m => m.PhoneNumber == phoneNumber, cancellationToken);

    public Task AddAsync(Member member, CancellationToken cancellationToken)
    {
        dbContext.Members.Add(member);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
