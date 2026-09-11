using ClosedXML.Excel;
using TaladPOS.Application.Reports;

namespace TaladPOS.Infrastructure.Reports;

/// <summary>ClosedXML-backed implementation of <see cref="IWorkbookExportService"/> (research.md items 1–2).</summary>
public class ClosedXmlWorkbookExportService : IWorkbookExportService
{
    // Excel worksheet names are capped at 31 characters and can't contain \ / ? * [ ].
    private const int MaxSheetNameLength = 31;

    public byte[] BuildXlsx(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SanitizeSheetName(sheetName));

        for (var column = 0; column < headers.Count; column++)
        {
            var headerCell = sheet.Cell(1, column + 1);
            headerCell.Value = headers[column];
            headerCell.Style.Font.Bold = true;
        }

        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var column = 0; column < row.Count; column++)
            {
                sheet.Cell(rowIndex, column + 1).Value = ToCellValue(row[column]);
            }

            rowIndex++;
        }

        if (headers.Count > 0)
        {
            sheet.Columns(1, headers.Count).AdjustToContents();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static XLCellValue ToCellValue(object? value) => value switch
    {
        null => Blank.Value,
        string s => s,
        bool b => b,
        int i => i,
        decimal d => d,
        double d => d,
        DateTime dt => dt,
        DateTimeOffset dto => dto.LocalDateTime,
        DateOnly date => date.ToDateTime(TimeOnly.MinValue),
        _ => value.ToString() ?? string.Empty,
    };

    private static string SanitizeSheetName(string sheetName)
    {
        var invalidChars = new[] { '\\', '/', '?', '*', '[', ']', ':' };
        var sanitized = new string(sheetName.Select(c => invalidChars.Contains(c) ? '-' : c).ToArray());
        return sanitized.Length > MaxSheetNameLength ? sanitized[..MaxSheetNameLength] : sanitized;
    }
}
