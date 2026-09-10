namespace TaladPOS.Application.Reports;

/// <summary>Daily/monthly total sales report (FR-029).</summary>
public class SalesSummaryQuery(IReportsRepository reportsRepository)
{
    public Task<IReadOnlyList<SalesSummaryPeriod>> ExecuteAsync(
        string granularity, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var monthly = granularity switch
        {
            "daily" => false,
            "monthly" => true,
            _ => throw new ArgumentException($"Unknown granularity: '{granularity}'. Expected 'daily' or 'monthly'.", nameof(granularity)),
        };

        return reportsRepository.GetSalesSummaryAsync(monthly, from, to, cancellationToken);
    }
}
