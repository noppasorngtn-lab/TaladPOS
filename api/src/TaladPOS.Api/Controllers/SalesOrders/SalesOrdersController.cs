using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.SalesOrders;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Api.Controllers.SalesOrders;

[ApiController]
[Route("sales-orders")]
[Authorize]
public class SalesOrdersController(
    CheckoutUseCase checkoutUseCase,
    VoidSalesOrderUseCase voidSalesOrderUseCase,
    PricingPreviewQuery pricingPreviewQuery,
    ISalesOrderRepository salesOrderRepository) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SalesOrderResponse>> Checkout(CheckoutRequest request, CancellationToken cancellationToken)
    {
        var staffId = User.GetStaffId();
        var lines = request.Lines.Select(l => new CheckoutLineRequest(l.ProductId, l.Quantity)).ToList();

        var order = await checkoutUseCase.ExecuteAsync(staffId, request.MemberId, lines, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, ToResponse(order));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesOrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        // A plain lookup by id doesn't warrant its own "use case" — full history search/filtering
        // is User Story 5's job (T059); this is just the single-record read for this endpoint.
        var order = await salesOrderRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"SalesOrder {id} was not found.");

        var isAdmin = User.IsInRole(StaffRole.Admin);
        if (!isAdmin && order.StaffId != User.GetStaffId())
        {
            return Forbid();
        }

        return Ok(ToResponse(order));
    }

    [HttpPost("{id:guid}/void")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<SalesOrderResponse>> Void(Guid id, CancellationToken cancellationToken)
    {
        var order = await voidSalesOrderUseCase.ExecuteAsync(id, cancellationToken);
        return Ok(ToResponse(order));
    }

    [HttpGet("pricing-preview")]
    public async Task<ActionResult<PricingPreviewResponse>> PricingPreview(
        [FromQuery] Guid? memberId, [FromQuery] Guid[] productId, [FromQuery] int[] quantity, CancellationToken cancellationToken)
    {
        if (productId.Length != quantity.Length)
        {
            throw new ArgumentException("productId and quantity arrays must be the same length.");
        }

        var lines = productId.Zip(quantity, (pid, qty) => new PricingPreviewLineRequest(pid, qty)).ToList();
        var result = await pricingPreviewQuery.ExecuteAsync(memberId, lines, cancellationToken);

        return Ok(new PricingPreviewResponse(result.SubtotalAmount, result.PromotionDiscountAmount, result.MemberDiscountAmount, result.NetTotal));
    }

    private static SalesOrderResponse ToResponse(SalesOrder order) => new(
        order.Id,
        order.CreatedAt,
        order.StaffId,
        order.MemberId,
        order.Status.ToString(),
        order.SubtotalAmount,
        order.PromotionDiscountAmount,
        order.MemberDiscountAmount,
        order.NetTotal,
        order.VoidedAt,
        order.Lines.Select(l => new SalesOrderLineResponse(
            l.ProductId, l.ProductNameSnapshot, l.UnitPriceSnapshot, l.Quantity, l.LineDiscountAmount, l.LineTotal)).ToList());
}
