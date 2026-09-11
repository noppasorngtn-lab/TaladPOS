# Contract: Sale History Export

Traceability: FR-002–FR-005, FR-007–FR-010 (User Story 2)

## GET /sales-orders/export

Downloads every sales order matching the given filters as an Excel file — the export counterpart
of the existing `GET /sales-orders` (paginated JSON search), but returns **every** matching row
regardless of page size (FR-004, research.md item 5), not just one page.

- **Auth**: Admin
- **Query params**: same filter set as `GET /sales-orders`, minus `page`/`pageSize` (there is no
  pagination to control — every match is included):
  - `from`, `to` (dates, optional)
  - `staffId` (optional)
  - `memberId` (optional)
  - `status` (optional, `Completed` | `Voided`)
- **Response 200**:
  - `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
  - `Content-Disposition: attachment; filename="sales-history-<from>-to-<to>.xlsx"` (or
    `sales-history-<generated-date>.xlsx` when no date filter is set)
  - One sheet, one header row: `Date | Staff | Member | Net total | Status`
  - One data row per matching sales order (Completed or Voided, whichever the `status` filter
    allows), newest first — matching the on-screen table's ordering
  - `Member` cell is blank for orders not linked to a member
- **Response 200 (no matches)**: header row only, zero data rows (FR-009)
- **Response 401/403**: standard JSON error shape (contracts/README.md) when the caller isn't an
  authenticated Admin (FR-008)
