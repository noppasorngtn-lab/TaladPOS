namespace TaladPOS.Application.Reports;

/// <summary>
/// Renders tabular data as an Excel (.xlsx) workbook (research.md item 1–2, feature
/// 002-export-reports-sales-history). Generic across every export in the system — it has no
/// knowledge of Products or SalesOrders, only headers and rows — so Domain/Application stay free
/// of any third-party spreadsheet library type, mirroring the existing IProductImageStore seam.
/// </summary>
public interface IWorkbookExportService
{
    byte[] BuildXlsx(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows);
}
