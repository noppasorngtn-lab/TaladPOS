using TaladPOS.Domain.Entities;

namespace TaladPOS.Application.SalesOrders;

public record SearchSalesOrdersResult(IReadOnlyList<SalesOrder> Items, int Total);

/// <summary>Sale history search for the Admin-only history screen (FR-026).</summary>
public class SearchSalesOrdersQuery(ISalesOrderRepository salesOrderRepository)
{
    public async Task<SearchSalesOrdersResult> ExecuteAsync(
        DateOnly? from,
        DateOnly? to,
        Guid? staffId,
        Guid? memberId,
        SalesOrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var (items, total) = await salesOrderRepository.SearchAsync(from, to, staffId, memberId, status, page, pageSize, cancellationToken);
        return new SearchSalesOrdersResult(items, total);
    }
}
