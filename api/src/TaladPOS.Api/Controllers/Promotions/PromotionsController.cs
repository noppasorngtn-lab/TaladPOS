using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Promotions;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Api.Controllers.Promotions;

[ApiController]
[Route("promotions")]
[Authorize]
public class PromotionsController(ManagePromotionUseCases managePromotionUseCases, IPromotionRepository promotionRepository) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<PromotionListResponse>> GetAll([FromQuery] bool activeOnly, CancellationToken cancellationToken)
    {
        var promotions = await promotionRepository.GetAllAsync(activeOnly, cancellationToken);
        return Ok(new PromotionListResponse(promotions.Select(ToResponse).ToList()));
    }

    // contracts/promotions.md: any authenticated staff, not just Admin — used by the Sales
    // screen to preview which promotions are in effect today (research.md item 5).
    [HttpGet("applicable")]
    public async Task<ActionResult<PromotionListResponse>> GetApplicable([FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var asOfDate = date ?? DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var promotions = await promotionRepository.GetActiveAsync(cancellationToken);
        var applicable = promotions.Where(p => p.CoversDate(asOfDate)).ToList();

        return Ok(new PromotionListResponse(applicable.Select(ToResponse).ToList()));
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<PromotionResponse>> Create(UpsertPromotionRequestDto request, CancellationToken cancellationToken)
    {
        var promotion = await managePromotionUseCases.CreateAsync(ToUseCaseRequest(request), cancellationToken);
        // No single-resource GET /promotions/{id} exists per contracts/promotions.md — the
        // management screen only ever lists (GET /promotions), so 201 has no Location to point to.
        return StatusCode(StatusCodes.Status201Created, ToResponse(promotion));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<PromotionResponse>> Update(Guid id, UpsertPromotionRequestDto request, CancellationToken cancellationToken)
    {
        var promotion = await managePromotionUseCases.EditAsync(id, ToUseCaseRequest(request), cancellationToken);
        return Ok(ToResponse(promotion));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await managePromotionUseCases.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    private static UpsertPromotionRequest ToUseCaseRequest(UpsertPromotionRequestDto request)
    {
        if (!Enum.TryParse<PromotionScope>(request.Scope, ignoreCase: true, out var scope))
        {
            throw new ArgumentException($"Unknown promotion scope: '{request.Scope}'.", nameof(request.Scope));
        }

        return new UpsertPromotionRequest(scope, request.DiscountPercent, request.StartDate, request.EndDate, request.ProductId);
    }

    private static PromotionResponse ToResponse(Promotion promotion) => new(
        promotion.Id, promotion.Scope.ToString(), promotion.ProductId, promotion.DiscountPercent,
        promotion.StartDate, promotion.EndDate, promotion.IsActive);
}
