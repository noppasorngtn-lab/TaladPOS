using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.Promotions;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>contracts/promotions.md GET /promotions — activeOnly filters to promotions whose
    /// date window covers today; otherwise every promotion (including deactivated ones) is returned.</summary>
    Task<IReadOnlyList<Promotion>> GetAllAsync(bool activeOnly, CancellationToken cancellationToken);

    /// <summary>All non-deactivated promotions, for pricing (research.md item 5). Date-window
    /// filtering (FR-023) is the caller's <see cref="Domain.Pricing.SalesOrderPricingService"/>'s job.</summary>
    Task<IReadOnlyList<Promotion>> GetActiveAsync(CancellationToken cancellationToken);

    Task AddAsync(Promotion promotion, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
