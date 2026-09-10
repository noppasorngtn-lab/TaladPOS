using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.Promotions;

public record UpsertPromotionRequest(PromotionScope Scope, decimal DiscountPercent, DateOnly StartDate, DateOnly EndDate, Guid? ProductId);

/// <summary>Promotion create/edit/deactivate for the Promotion Management screen (FR-020–FR-022).</summary>
public class ManagePromotionUseCases(IPromotionRepository promotionRepository)
{
    public async Task<Promotion> CreateAsync(UpsertPromotionRequest request, CancellationToken cancellationToken)
    {
        var promotion = new Promotion(request.Scope, request.DiscountPercent, request.StartDate, request.EndDate, request.ProductId);

        await promotionRepository.AddAsync(promotion, cancellationToken);
        await promotionRepository.SaveChangesAsync(cancellationToken);
        return promotion;
    }

    public async Task<Promotion> EditAsync(Guid id, UpsertPromotionRequest request, CancellationToken cancellationToken)
    {
        var promotion = await promotionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Promotion {id} was not found.");

        promotion.Edit(request.Scope, request.DiscountPercent, request.StartDate, request.EndDate, request.ProductId);
        await promotionRepository.SaveChangesAsync(cancellationToken);
        return promotion;
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var promotion = await promotionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Promotion {id} was not found.");

        promotion.Deactivate();
        await promotionRepository.SaveChangesAsync(cancellationToken);
    }
}
