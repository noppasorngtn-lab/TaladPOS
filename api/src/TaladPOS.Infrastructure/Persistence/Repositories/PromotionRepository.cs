using Microsoft.EntityFrameworkCore;
using TaladPOS.Application.Promotions;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Infrastructure.Persistence.Repositories;

public class PromotionRepository(TaladPOSDbContext dbContext) : IPromotionRepository
{
    public Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Promotions.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Promotion>> GetAllAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.Promotions.AsQueryable();

        if (activeOnly)
        {
            var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
            query = query.Where(p => p.IsActive && p.StartDate <= today && p.EndDate >= today);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Promotion>> GetActiveAsync(CancellationToken cancellationToken) =>
        await dbContext.Promotions.Where(p => p.IsActive).ToListAsync(cancellationToken);

    public Task AddAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        dbContext.Promotions.Add(promotion);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
