# Contract: Stock Levels Export

Traceability: FR-001, FR-003, FR-006, FR-008–FR-010 (User Story 1)

## GET /reports/stock-levels/export

Downloads every currently-active product's stock level as an Excel file — the export counterpart
of the existing `GET /reports/stock-levels` JSON endpoint, sharing the same underlying query
(`StockLevelsQuery`, research.md item 6) so the two can never disagree on which rows are included.

- **Auth**: Admin
- **Query params**: none — this report has no date filter on-screen either (spec.md Assumptions),
  so the export always covers the full current stock-level list
- **Response 200**:
  - `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
  - `Content-Disposition: attachment; filename="stock-levels-<YYYY-MM-DD>.xlsx"` (date = when the
    export was generated)
  - One sheet, one header row: `Product name | Quantity on hand | Low stock`
  - One data row per active product, sorted by name (matching the on-screen table's ordering)
- **Response 200 (empty catalog)**: header row only, zero data rows (FR-009)
- **Response 401/403**: standard JSON error shape (contracts/README.md) when the caller isn't an
  authenticated Admin (FR-008)
