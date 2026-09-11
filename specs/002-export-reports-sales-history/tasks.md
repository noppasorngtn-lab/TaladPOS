---

description: "Task list template for feature implementation"
---

# Tasks: Export รายงานสินค้าคงเหลือและประวัติการขาย

**Input**: Design documents from `/specs/002-export-reports-sales-history/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md (all present)

**Tests**: This feature adds no new Domain-layer business rules (data-model.md), so Constitution
Principle III's mandatory-xUnit gate isn't triggered the way it was for pricing/stock/void logic in
`001-pos-system`. The concrete required tests here are **API integration tests**
(`TaladPOS.Api.IntegrationTests`), matching how the existing `/reports/*` endpoints are already
verified in `ReportsTests.cs` (plan.md "Testing"). They are written before their corresponding
implementation task in each story, expected to fail (404, since the route doesn't exist yet) until
that story's implementation task lands — same red→green spirit `001-pos-system/tasks.md` used for
its Domain unit tests, just at the API-contract level since there's no new Domain logic here to
unit-test in isolation.

**Organization**: Tasks are grouped by user story (US1 = Priority P1, US2 = Priority P2) so each
can be implemented, tested, and demoed independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: Which user story this task belongs to (US1, US2)
- File paths below are relative to the repository root

## Path Conventions

This feature extends the existing `api/` (ASP.NET Core, DDD-layered) and `web/` (Next.js) split
from `001-pos-system` — no new top-level directory. See `plan.md` → Project Structure for the full
tree.

---

## Phase 1: Setup

**Purpose**: Add the one new third-party dependency this feature needs.

- [X] T001 Add `ClosedXML` package version entry to `api/Directory.Packages.props` and a
      `<PackageReference Include="ClosedXML" />` to
      `api/src/TaladPOS.Infrastructure/TaladPOS.Infrastructure.csproj` (research.md item 1 —
      MIT-licensed, no other project should reference it directly)

**Checkpoint**: `dotnet restore` succeeds with ClosedXML available to `TaladPOS.Infrastructure`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The shared workbook-building seam and frontend download plumbing both user stories
depend on.

**🚨 CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Define `IWorkbookExportService` in
      `api/src/TaladPOS.Application/Reports/IWorkbookExportService.cs` with a method shaped
      `byte[] BuildXlsx(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)`,
      implement it with ClosedXML in
      `api/src/TaladPOS.Infrastructure/Reports/ClosedXmlWorkbookExportService.cs` (one sheet, bold
      header row, auto-fit columns, empty `rows` still produces a valid file with just the header —
      FR-009), and register `builder.Services.AddSingleton<IWorkbookExportService, ClosedXmlWorkbookExportService>();`
      in `api/src/TaladPOS.Api/Program.cs` (research.md items 1–2; depends on T001)
- [X] T003 [P] Add a blob-aware fetch helper (e.g. `apiFetchBlob`) to `web/src/lib/api/client.ts`
      that attaches the same `Authorization: Bearer <token>` header as `apiFetch`, parses the
      existing `{ error: { code, message } }` JSON shape on any non-2xx response into an `ApiError`
      (same as `apiFetch`), and otherwise returns `response.blob()`; and add a new
      `web/src/lib/download.ts` exporting `downloadBlob(blob: Blob, filename: string): void` that
      creates a temporary `<a>` element via `URL.createObjectURL`, clicks it, and revokes the URL
      (research.md item 3)

**Checkpoint**: Both the backend workbook-building seam and the frontend download plumbing exist
and compile. User story implementation can now begin.

---

## Phase 3: User Story 1 - Export รายงานสินค้าคงเหลือ (Priority: P1) 🎯 MVP

**Goal**: An Admin on the Reports screen can click Export next to "Stock levels" and download an
`.xlsx` file listing every active product's name, quantity on hand, and low-stock flag.

**Independent Test**: `quickstart.md` § 1 — works standalone, no dependency on User Story 2.

### Tests for User Story 1

- [X] T004 [P] [US1] Add integration tests to `api/tests/TaladPOS.Api.IntegrationTests/ReportsTests.cs`
      for `GET /reports/stock-levels/export` (contracts/stock-export.md): an Admin gets `200` with
      `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` and a
      non-empty body containing a product just created via the API; a `Cashier`-role token gets
      `403` (mirrors the existing `SecurityTests.cs` pattern for other Admin-only endpoints). These
      should fail (404 — route doesn't exist yet) until T005 lands.

### Implementation for User Story 1

- [X] T005 [US1] Add a `[HttpGet("stock-levels/export")]` action to
      `api/src/TaladPOS.Api/Controllers/Reports/ReportsController.cs` (inherits the class-level
      `[Authorize(Policy = "Admin")]` already on this controller): call the existing
      `StockLevelsQuery`, map each item to a row `[ProductName, QuantityOnHand, LowStock ? "Low stock" : ""]`,
      call `IWorkbookExportService.BuildXlsx("Stock levels", ["Product name", "Quantity on hand", "Low stock"], rows)`,
      and return `File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"stock-levels-{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}.xlsx")`
      (contracts/stock-export.md; depends on T002, T004)
- [X] T006 [P] [US1] Add `exportStockLevels(token: string): Promise<Blob>` to
      `web/src/lib/api/reports.ts` calling `apiFetchBlob(token, "/reports/stock-levels/export")`
      (depends on T003)
- [X] T007 [US1] In `web/src/app/(app)/reports/page.tsx`, add an "Export" button next to the "Stock
      levels" section heading: on click, disable the button (FR-010), call `exportStockLevels`,
      pass the result to `downloadBlob(blob, `stock-levels-${today()}.xlsx`)` (reuse the existing
      `today()` helper already in this file), re-enable the button in a `finally` block, and surface
      any `ApiError` using the same error-banner pattern the page already uses for `refresh()`
      failures (depends on T003, T006)

**Checkpoint**: User Story 1 is fully functional and independently testable/demoable — this is the
MVP. Stop here and validate before continuing if delivering incrementally.

---

## Phase 4: User Story 2 - Export ประวัติการขาย (Priority: P2)

**Goal**: An Admin on the Sale History screen can set date/status filters, click Export, and
download an `.xlsx` file with one row per matching order (date, staff name, member name, net
total, status) covering *every* matching order, not just the current on-screen page.

**Independent Test**: `quickstart.md` § 2 — works standalone, no dependency on User Story 1.

### Tests for User Story 2

- [X] T008 [P] [US2] Add integration tests to
      `api/tests/TaladPOS.Api.IntegrationTests/SalesOrdersTests.cs` for `GET /sales-orders/export`
      (contracts/sales-history-export.md): create enough orders (some `Completed`, at least one
      `Voided`, at least one linked to a member) that the filtered result exceeds one page's worth
      of the existing paginated `GET /sales-orders` default `pageSize` — assert the exported file's
      row count is *greater than* what one page of `GET /sales-orders` would return for the same
      filter (proves FR-004, not just that the endpoint returns *something*); assert a row for the
      member-linked order has a non-blank member name and a row for a non-linked order has a blank
      one; assert a `status=Voided`-filtered export contains only voided rows; assert a filter with
      zero matches still returns `200` with a header-only file (FR-009); assert a `Cashier`-role
      token gets `403`. These should fail (404) until T010 lands.

### Implementation for User Story 2

- [X] T009 [US2] Add `SalesHistoryExportRow(DateTimeOffset CreatedAt, string StaffName, string? MemberName, decimal NetTotal, string Status)`
      and a `SearchAllForExportAsync(DateOnly? from, DateOnly? to, Guid? staffId, Guid? memberId, SalesOrderStatus? status, CancellationToken)`
      method to `api/src/TaladPOS.Application/SalesOrders/ISalesOrderRepository.cs`; implement it in
      `api/src/TaladPOS.Infrastructure/Persistence/Repositories/SalesOrderRepository.cs` by applying
      the same filters `SearchAsync` already applies (minus `Skip`/`Take`), then batch-resolving
      `StaffName`/`MemberName` via `dbContext.Staff`/`dbContext.Members` dictionaries exactly the
      way `ReportsRepository.GetSalesByStaffAsync` already does (research.md item 4) — `MemberName`
      is `null` when `MemberId` is `null`; add a thin `ExportSalesHistoryQuery` in
      `api/src/TaladPOS.Application/SalesOrders/ExportSalesHistoryQuery.cs` that just calls the new
      repository method (same shape as the existing `SearchSalesOrdersQuery`), and register it in
      `api/src/TaladPOS.Api/Program.cs` (depends on T002)
- [X] T010 [US2] Add a `[HttpGet("export")]` action to
      `api/src/TaladPOS.Api/Controllers/SalesOrders/SalesOrdersController.cs` guarded by
      `[Authorize(Policy = "Admin")]` (same query params as the existing `Search` action minus
      `page`/`pageSize`): call `ExportSalesHistoryQuery`, map rows to
      `[CreatedAt.ToString(), StaffName, MemberName ?? "", NetTotal.ToString("0.00"), Status]`, call
      `IWorkbookExportService.BuildXlsx("Sale history", ["Date", "Staff", "Member", "Net total", "Status"], rows)`,
      and return the file with a `Content-Disposition` filename derived from the `from`/`to`
      params when present, else the generation date (contracts/sales-history-export.md; depends on
      T008, T009)
- [X] T011 [P] [US2] Add `exportSalesHistory(token: string, filters: SalesOrderSearchFilters): Promise<Blob>`
      to `web/src/lib/api/salesOrders.ts`, reusing the same query-string-building logic
      `searchSalesOrders` already has (minus `page`/`pageSize`) and calling
      `apiFetchBlob(token, `/sales-orders/export?${params}`)` (depends on T003)
- [X] T012 [US2] In `web/src/app/(app)/sales-history/page.tsx`, add an "Export" button next to the
      existing "Clear filters" button: on click, disable the button (FR-010), call
      `exportSalesHistory(token, filters)` with the currently-applied filters, pass the result to
      `downloadBlob`, re-enable the button in a `finally` block, and surface any `ApiError` using
      the same error-banner pattern the page already uses (depends on T003, T011)

**Checkpoint**: Both User Stories are now independently functional. This is the full feature.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [ ] T013 [P] Run the full `quickstart.md` validation walkthrough (all three sections) against a
      locally running `api/` + `web/`, confirming both files open correctly in Excel with Thai
      characters intact (SC-003) and both Export buttons correctly disable during the download
      (Edge Cases). **Blocked in this session**: both available browser-automation tools
      (Playwright MCP, Claude-in-Chrome) were unusable in this environment (stale profile lock /
      extension not connected) — `api/` (new code) and `web/` are both running locally
      (`http://localhost:3000`, `http://localhost:5260`) for whoever picks this up to finish
      manually. Automated coverage already proves the same acceptance criteria at the HTTP/data
      layer (T004, T008 — 7 passing integration tests covering content-type, FR-004's
      more-than-one-page row count, FR-009's empty/header-only file, and 403-for-Cashier); what's
      left here is purely the visual/UX confirmation a human or a working browser tool would add.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup (T001). **Blocks all user stories.**
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) only.
- **User Story 2 (Phase 4)**: Depends on Foundational (Phase 2) only — **not** on User Story 1;
  the two exports share no code path beyond `IWorkbookExportService`/`apiFetchBlob`/`downloadBlob`
  built in Phase 2.
- **Polish (Phase 5)**: Depends on both user stories being complete.

### Within Each User Story

- Integration test task before its story's implementation tasks (red → green).
- Backend (repository → query → controller) before the frontend client function.
- Frontend client function before the page wiring that calls it.

### Parallel Opportunities

- T002 and T003 (Phase 2) touch entirely different projects (`api/` vs `web/`) — run together.
- T004 (US1 test) and T008 (US2 test) touch different test files — can be written together once
  Phase 2 is done, even before deciding which story to implement first.
- T006 (US1 frontend client) and T011 (US2 frontend client) touch different files — parallelizable.
- User Story 1 and User Story 2 have no cross-dependency once Phase 2 is done — a second developer
  could start Phase 4 immediately after Phase 2, without waiting for Phase 3.

---

## Parallel Example: Phase 2 (Foundational)

```bash
Task: "Define IWorkbookExportService + ClosedXmlWorkbookExportService, register in Program.cs"
Task: "Add apiFetchBlob to client.ts + downloadBlob helper in download.ts"
```

## Parallel Example: User Story 1

```bash
# After Phase 2 completes:
Task: "Integration tests for GET /reports/stock-levels/export in ReportsTests.cs"
# Then, once that test file exists:
Task: "Add stock-levels/export action to ReportsController.cs"
Task: "Add exportStockLevels(token) to reports.ts"  # can run alongside the controller task
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (Setup) and Phase 2 (Foundational).
2. Complete Phase 3 (User Story 1 — stock levels export).
3. **Stop and validate** with `quickstart.md` § 1.
4. This alone is a shippable increment: Admins can already export stock levels even before Sale
   History export exists.

### Incremental Delivery

1. Setup + Foundational → shared seams exist.
2. Add User Story 1 → validate independently → ship (MVP).
3. Add User Story 2 → validate independently → ship.
4. Polish (quickstart.md full pass).

### Total Task Count

13 tasks: 1 Setup, 2 Foundational, 4 for User Story 1, 4 for User Story 2, 1 Polish, plus this
being a small feature means there's no separate "team split" strategy needed — one developer can
reasonably work through Phases 1→5 in order in a single sitting.
