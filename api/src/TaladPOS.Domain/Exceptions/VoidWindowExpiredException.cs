namespace TaladPOS.Domain.Exceptions;

/// <summary>Raised when voiding is attempted after the same-day window has passed (FR-027).</summary>
public class VoidWindowExpiredException(Guid salesOrderId)
    : DomainConflictException("void_window_expired", $"SalesOrder {salesOrderId} can only be voided on the same calendar day it was created.")
{
    public Guid SalesOrderId { get; } = salesOrderId;
}
