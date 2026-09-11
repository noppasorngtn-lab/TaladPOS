using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.SalesOrders;

/// <summary>
/// Sale history export for the Admin-only history screen (feature
/// 002-export-reports-sales-history, FR-002–FR-005). A thin pass-through to
/// <see cref="ISalesOrderRepository.SearchAllForExportAsync"/> — same shape as
/// <see cref="SearchSalesOrdersQuery"/>, just without paging (research.md item 5).
/// </summary>
public class ExportSalesHistoryQuery(ISalesOrderRepository salesOrderRepository)
{
    public Task<IReadOnlyList<SalesHistoryExportRow>> ExecuteAsync(
        DateOnly? from, DateOnly? to, Guid? staffId, Guid? memberId, SalesOrderStatus? status, CancellationToken cancellationToken) =>
        salesOrderRepository.SearchAllForExportAsync(from, to, staffId, memberId, status, cancellationToken);
}
