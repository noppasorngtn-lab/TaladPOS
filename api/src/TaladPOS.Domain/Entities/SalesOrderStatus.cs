namespace TaladPOS.Domain.Entities;

/// <summary>One-way lifecycle: a SalesOrder starts Completed and may transition to Voided (FR-027/FR-028).</summary>
public enum SalesOrderStatus
{
    Completed,
    Voided,
}
