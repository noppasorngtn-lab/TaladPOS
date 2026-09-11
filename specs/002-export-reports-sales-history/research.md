# Phase 0 Research: Export รายงานสินค้าคงเหลือและประวัติการขาย

All Technical Context unknowns for this feature are resolved below — there are no genuinely open
`NEEDS CLARIFICATION` items left after `/speckit-clarify` (see spec.md `## Clarifications`), so this
phase focuses on **how** to implement what was already decided **what** of, consistent with the
existing `001-pos-system` codebase's established patterns.

## 1. Excel (.xlsx) generation library

**Decision**: Use **ClosedXML** (MIT license) as the only new backend dependency, added to
`api/Directory.Packages.props` and referenced only from `TaladPOS.Infrastructure`.

**Rationale**: The spec's clarified format is Excel (.xlsx), not CSV, so a real workbook-writing
library is needed. ClosedXML offers a small, fluent, well-documented API well suited to "headers +
rows" tabular sheets like both exports in this feature — no need for its charting/formula/pivot
features. It's MIT-licensed with no commercial-use restriction, which matters for a product that
will run at real stores.

**Alternatives considered**:
- **EPPlus** — very popular, but versions ≥5 ship under Polyform Noncommercial, requiring a paid
  commercial license for exactly this use case (a for-sale POS product). Rejected on licensing risk
  alone.
- **DocumentFormat.OpenXml** (Microsoft's own SDK) — MIT-licensed and would avoid a third-party
  dependency entirely, but its API operates at the raw OOXML-part level; building even a simple
  sheet requires noticeably more boilerplate than this feature's scope justifies.
- **NPOI** (Apache POI port) — Apache 2.0, functionally capable, but its API is older-style and less
  ergonomic for this codebase's conventions than ClosedXML's fluent builder.

## 2. Keeping the Excel library out of Domain/Application

**Decision**: Introduce one small interface in `TaladPOS.Application` —
`IWorkbookExportService` — with a generic `BuildXlsx(sheetName, headers, rows)`-shaped method,
implemented in `TaladPOS.Infrastructure` using ClosedXML. Both export use cases (stock levels,
sales history) depend on this interface, never on ClosedXML types directly.

**Rationale**: Constitution Principle II requires Domain (and by established precedent in this
codebase, Application too — see `IProductImageStore`) to stay free of infrastructure-specific
library types, so the persistence/tooling choice can change without touching business logic. This
mirrors the exact seam already used for product image storage (`IProductImageStore` in
Application, `ProductImageStore` in Infrastructure) — same shape of problem, same solution.

**Alternatives considered**: Referencing ClosedXML directly from the Api or Application layer —
rejected because it breaks an already-accepted pattern in this exact codebase for no benefit; a
generic interface costs almost nothing here since both exports are simple flat tables.

## 3. How the browser downloads a binary file through an authenticated API

**Decision**: Add one small blob-aware fetch helper (e.g. `apiFetchBlob`) alongside the existing
`apiFetch` in `web/src/lib/api/client.ts`, and a tiny `downloadBlob(blob, filename)` DOM helper
(temporary `<a>` element + `URL.createObjectURL`, then revoke the URL). The Export buttons call the
resource-specific client function (which calls `apiFetchBlob`), then pass the result to
`downloadBlob`.

**Rationale**: Every existing endpoint returns JSON, so `apiFetch` always calls `response.json()`
and always sends `Content-Type: application/json`. The two new export endpoints return a binary
`.xlsx` body instead, so they need a response handler that calls `response.blob()` and skips the
JSON error-parsing path (a non-2xx export response is still JSON-shaped
`{ error: { code, message } }` per the existing error convention and should be parsed as such
before falling back to blob handling). This is additive — it does not change `apiFetch` or any
existing caller.

**Alternatives considered**: Navigating the browser directly to the export URL
(`window.location.href = ...`) — rejected because the JWT is kept in `localStorage` and attached as
an `Authorization` header by `apiFetch`; a plain browser navigation cannot attach a custom header,
so the request would arrive unauthenticated and be rejected by `[Authorize]`.

## 4. Resolving staff/member names for the sales-history export

**Decision**: Follow the exact pattern `ReportsRepository.GetSalesByStaffAsync` already
establishes for `StaffName` — including *which layer* does the resolving. That method lives in
Infrastructure, queries matching orders, collects the distinct `StaffId` values, batch-resolves
`dbContext.Staff` into a `Dictionary<Guid, string>`, and returns an already-name-resolved DTO
(`StaffSalesItem`) straight out of `IReportsRepository` — the Application layer never sees a raw
`SalesOrder` or does any resolving itself. The new export path copies this shape exactly: a new
`ISalesOrderRepository.SearchAllForExportAsync(...)` method (Decision 5) does the filtering *and*
the `StaffId`/`MemberId` → name resolution inside `SalesOrderRepository` (Infrastructure, where
`TaladPOSDbContext` is available), and returns a ready-to-render `SalesHistoryExportRow` DTO
(data-model.md) directly.

**Rationale**: `SalesOrder` (Domain entity) only stores `StaffId`/`MemberId` GUIDs — names live on
`Staff`/`Member`, and neither `IStaffRepository` nor `IMemberRepository` currently exposes a
batch-by-ids lookup for the Application layer to call instead. Rather than adding one, mirroring
`ReportsRepository`'s existing shape (resolve inside the Infrastructure repository, return a
finished DTO) keeps the layering identical to already-reviewed code solving the same problem, and
keeps the Application-layer query a thin pass-through — consistent with how
`SearchSalesOrdersQuery` (the existing, non-export search) is also just a thin call into
`ISalesOrderRepository.SearchAsync`.

**Alternatives considered**: Adding batch `GetByIdsAsync`-style methods to `IStaffRepository`/
`IMemberRepository` so the Application layer could resolve names itself — rejected as unnecessary
indirection for a single call site, when the exact problem already has a working, reviewed
solution one method away in `ReportsRepository`. Adding an EF `Include`/join directly into
`SalesOrderRepository.SearchAsync` (the paginated, on-screen method) — rejected because that method
backs the on-screen Sale History list, which never needs to display names today; joining there
would add unnecessary query cost to the common (screen-rendering) path to serve the uncommon
(export) path.

## 5. A dedicated "no pagination" export query

**Decision**: Add a new repository method, `SearchAllForExportAsync(...)`, with the same filter
parameters as the existing `SearchAsync` (`from`, `to`, `staffId`, `memberId`, `status`) but no
`page`/`pageSize` — it returns every matching row, already shaped as `SalesHistoryExportRow`
(Decision 4). A new Application-layer `ExportSalesHistoryQuery` is a thin pass-through that calls
it — it exists mainly so the Controller depends on an Application-layer type per this codebase's
convention (Controllers call Application queries/use-cases, never repositories directly), not
because there's meaningful orchestration logic to add on top.

**Rationale**: The spec (FR-004, confirmed in `/speckit-clarify`) requires the export to cover
every filtered row, not just the current on-screen page. Keeping this as a distinct method (rather
than calling `SearchAsync` with a very large `pageSize`) makes the "get everything matching" intent
explicit in the code and immune to whatever page-size cap the on-screen search might adopt later.

**Alternatives considered**: Raising `SearchAsync`'s effective max page size high enough to act as
"export everything" — rejected because it conflates two different call sites' intent (bounded
on-screen browsing vs. intentionally-unbounded export) behind one ambiguous parameter.

## 6. Stock-levels export reuses the existing query as-is

**Decision**: No new Application-layer query is needed for the stock export — the existing
`StockLevelsQuery` (`api/src/TaladPOS.Application/Reports/`) already returns exactly the fields
FR-006 requires (`ProductName`, `QuantityOnHand`, `LowStock`). The new
`GET /reports/stock-levels/export` endpoint calls the same `StockLevelsQuery` the on-screen
`GET /reports/stock-levels` endpoint already uses, then hands the result to
`IWorkbookExportService` instead of a JSON `Ok(...)`.

**Rationale**: Avoids duplicating a query that already does exactly what's needed — the only new
work for this half of the feature is the workbook-building step and the controller action.

## 7. No export audit/history persistence in this version

**Decision**: The export endpoints are pure reads with no side effects — no new table, no logging
of "who exported what, when" beyond whatever the platform's normal request logging already
captures.

**Rationale**: Explicitly out of scope per spec.md Assumptions ("ไฟล์ export ไม่จำเป็นต้องถูกเก็บ
บันทึกไว้ในระบบหลังดาวน์โหลด").

**Alternatives considered**: A dedicated export-audit table — deferred, not rejected outright; if a
future compliance need arises (e.g., knowing which admin exported member phone numbers and when),
it can be added later without changing the design here, since it would be purely additive logging
around the same two endpoints.
