namespace TaladPOS.Domain.Exceptions;

/// <summary>Raised when attempting to void a SalesOrder that is already Voided (FR-028).</summary>
public class AlreadyVoidedException(Guid salesOrderId)
    : DomainConflictException("already_voided", $"SalesOrder {salesOrderId} has already been voided.")
{
    public Guid SalesOrderId { get; } = salesOrderId;
}
