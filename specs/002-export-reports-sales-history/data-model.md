# Phase 1 Data Model: Export รายงานสินค้าคงเหลือและประวัติการขาย

This feature adds **no new persisted tables or columns**. Both exports are read-only projections
over existing entities (`Product`, `SalesOrder`, `Staff`, `Member` — all already defined in
`api/src/TaladPOS.Domain/Entities/`). The two "Key Entities" the spec names
(`Stock Export Row`, `Sales History Export Row`) are transient response/DTO shapes, not EF Core
entities — nothing here needs a migration.

## Stock Export Row

Source: existing `StockLevelItem` (`api/src/TaladPOS.Application/Reports/IReportsRepository.cs`),
unchanged — the export reuses this record as-is (see research.md item 6).

| Field | Type | Derived from | Notes |
|---|---|---|---|
| `ProductName` | string | `Product.Name` | |
| `QuantityOnHand` | int | `Product.QuantityOnHand` | |
| `LowStock` | bool | `Product.IsLowStock` (via `LowStockThreshold`) | Rendered as a plain "Low stock" / blank text column in the sheet, matching the on-screen badge |

Excludes `Product.Id`, `ImageUrl`, `Barcode`, `Price`, `IsActive`, `RowVersion` — not part of what
the on-screen Stock levels table (or FR-006) shows, so not part of the export either.

## Sales History Export Row

**New** shape — does not exist yet. Extends what `SalesOrderSummary`
(`api/src/TaladPOS.Application/SalesOrders/`) already carries with the two resolved names
Clarification Q4 added.

| Field | Type | Derived from | Notes |
|---|---|---|---|
| `CreatedAt` | DateTimeOffset | `SalesOrder.CreatedAt` | Rendered in the sheet using the server's local presentation, consistent with how the on-screen Sale History table formats it (`toLocaleString()`) |
| `StaffName` | string | `Staff.Name`, resolved by `StaffId` (research.md item 4) | Falls back to `"(unknown)"` if the staff record is ever missing — same fallback `ReportsRepository.GetSalesByStaffAsync` already uses |
| `MemberName` | string? | `Member.Name`, resolved by `MemberId` (research.md item 4) | Blank cell when `MemberId` is `null` (not linked to a member) |
| `NetTotal` | decimal | `SalesOrder.NetTotal` | Same value already shown on-screen |
| `Status` | string | `SalesOrder.Status` (`Completed` \| `Voided`) | Same two values already shown on-screen |

Excludes `SubtotalAmount`, `PromotionDiscountAmount`, `MemberDiscountAmount`, `VoidedAt`, and line
items — FR-005 (confirmed in Clarifications) scopes this export to one summary row per order, not
a full bill breakdown.

## New Application-layer contracts (interfaces only, no schema)

```
IWorkbookExportService.BuildXlsx(sheetName: string, headers: string[], rows: IEnumerable<object?[]>): byte[]
```
Implemented in Infrastructure with ClosedXML (research.md items 1–2). Generic across both exports
— it has no knowledge of `Product` or `SalesOrder`, it just writes whatever headers/rows it's
given onto one sheet.

```
ISalesOrderRepository.SearchAllForExportAsync(from, to, staffId, memberId, status, cancellationToken)
  → IReadOnlyList<SalesHistoryExportRow>
```
Same filter shape as the existing `SearchAsync`, minus `page`/`pageSize` (research.md item 5). Unlike
`SearchAsync` (which returns `SalesOrder` domain entities for the paginated on-screen list), this
method returns the row already shaped and name-resolved (research.md item 4) — same division of
responsibility as `IReportsRepository`'s methods, which also return ready-to-render DTOs rather
than raw entities.
