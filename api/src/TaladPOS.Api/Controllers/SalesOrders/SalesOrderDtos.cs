namespace TaladPOS.Api.Controllers.SalesOrders;

public record SalesOrderLineDto(Guid ProductId, int Quantity);

public record CheckoutRequest(Guid? MemberId, List<SalesOrderLineDto> Lines);

public record PricingPreviewRequest(Guid? MemberId, List<SalesOrderLineDto> Lines);

public record SalesOrderLineResponse(
    Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineDiscountAmount, decimal LineTotal);

public record SalesOrderResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    Guid StaffId,
    Guid? MemberId,
    string Status,
    decimal SubtotalAmount,
    decimal PromotionDiscountAmount,
    decimal MemberDiscountAmount,
    decimal NetTotal,
    DateTimeOffset? VoidedAt,
    List<SalesOrderLineResponse> Lines);

public record PricingPreviewResponse(decimal SubtotalAmount, decimal PromotionDiscountAmount, decimal MemberDiscountAmount, decimal NetTotal);

public record SalesOrderSummaryResponse(
    Guid Id, DateTimeOffset CreatedAt, Guid StaffId, Guid? MemberId, string Status, decimal NetTotal);

public record SalesOrderSearchResponse(List<SalesOrderSummaryResponse> Items, int Total);
