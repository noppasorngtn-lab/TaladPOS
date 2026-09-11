using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.SalesOrders;

/// <summary>
/// One row of the Sale History export (feature 002-export-reports-sales-history, FR-005/FR-007) —
/// already name-resolved, the same division of responsibility <c>IReportsRepository</c> uses for
/// its DTOs, so the Application layer never needs to join Staff/Members itself (research.md item 4).
/// </summary>
public record SalesHistoryExportRow(DateTimeOffset CreatedAt, string StaffName, string? MemberName, decimal NetTotal, SalesOrderStatus Status);

public interface ISalesOrderRepository
{
    Task AddAsync(SalesOrder order, CancellationToken cancellationToken);

    /// <summary>Includes Lines — callers (checkout/void) always need the full aggregate.</summary>
    Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Sale history search (FR-026); any filter left null is not applied.</summary>
    Task<(IReadOnlyList<SalesOrder> Items, int Total)> SearchAsync(
        DateOnly? from,
        DateOnly? to,
        Guid? staffId,
        Guid? memberId,
        SalesOrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Same filters as <see cref="SearchAsync"/>, minus paging — every matching order, for the
    /// Sale History export (FR-004). Kept as a distinct method rather than a large page size so
    /// the "export everything matching" intent stays explicit (research.md item 5).
    /// </summary>
    Task<IReadOnlyList<SalesHistoryExportRow>> SearchAllForExportAsync(
        DateOnly? from,
        DateOnly? to,
        Guid? staffId,
        Guid? memberId,
        SalesOrderStatus? status,
        CancellationToken cancellationToken);
}
