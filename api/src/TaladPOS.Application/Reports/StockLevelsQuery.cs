namespace TaladPOS.Application.Reports;

/// <summary>Current on-hand quantity for every active product (FR-032).</summary>
public class StockLevelsQuery(IReportsRepository reportsRepository)
{
    public Task<IReadOnlyList<StockLevelItem>> ExecuteAsync(CancellationToken cancellationToken) =>
        reportsRepository.GetStockLevelsAsync(cancellationToken);
}
