namespace TaladPOS.Api.Controllers.Promotions;

public record UpsertPromotionRequestDto(string Scope, Guid? ProductId, decimal DiscountPercent, DateOnly StartDate, DateOnly EndDate);

public record PromotionResponse(Guid Id, string Scope, Guid? ProductId, decimal DiscountPercent, DateOnly StartDate, DateOnly EndDate, bool IsActive);

public record PromotionListResponse(List<PromotionResponse> Items);
