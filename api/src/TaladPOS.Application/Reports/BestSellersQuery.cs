namespace TaladPOS.Application.Reports;

/// <summary>Best-selling products by revenue over a date range (FR-030).</summary>
public class BestSellersQuery(IReportsRepository reportsRepository)
{
    public Task<IReadOnlyList<BestSellerItem>> ExecuteAsync(DateOnly from, DateOnly to, int limit, CancellationToken cancellationToken) =>
        reportsRepository.GetBestSellersAsync(from, to, limit, cancellationToken);
}
