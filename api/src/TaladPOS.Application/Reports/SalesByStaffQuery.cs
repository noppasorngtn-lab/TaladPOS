namespace TaladPOS.Application.Reports;

/// <summary>Sales totals grouped by staff member (FR-031).</summary>
public class SalesByStaffQuery(IReportsRepository reportsRepository)
{
    public Task<IReadOnlyList<StaffSalesItem>> ExecuteAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        reportsRepository.GetSalesByStaffAsync(from, to, cancellationToken);
}
