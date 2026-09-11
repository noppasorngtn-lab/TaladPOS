# Implementation Plan: Export รายงานสินค้าคงเหลือและประวัติการขาย

**Branch**: `002-export-reports-sales-history` | **Date**: 2026-09-11 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-export-reports-sales-history/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Add a one-click Excel (.xlsx) export to two existing Admin-only screens: the "Stock levels"
section of **Reports** (full current stock snapshot, no filters — User Story 1), and **Sale
History** (every order matching whatever date/status filter is currently set, not just the visible
page — User Story 2). Both exports are pure reads over data the API already serves as JSON today;
this feature adds two new endpoints that render the same underlying query results as `.xlsx`
instead, plus the two Export buttons and download plumbing on the frontend. No new tables, no new
persisted state, and no change to any existing endpoint's behavior.

## Technical Context

**Language/Version**: Backend: C# 13 / .NET 10 (matches the already-running `api/` project's
actual `TargetFramework`, verified in each `.csproj`). Frontend: TypeScript 5 / Node.js 20 LTS
(unchanged from `001-pos-system`).

**Primary Dependencies**: Backend: adds **ClosedXML** (MIT license, research.md item 1) as the only
new package, referenced solely from `TaladPOS.Infrastructure`; everything else (ASP.NET Core Web
API, EF Core 10, `Npgsql.EntityFrameworkCore.PostgreSQL`, JWT auth) is already in place and
unchanged. Frontend: no new npm package — the download is implemented with a small `fetch`+`Blob`
helper (research.md item 3), not a client-side spreadsheet library.

**Storage**: PostgreSQL 16, unchanged schema — both exports read existing `Products`, `SalesOrders`,
`Staff`, and `Members` tables (data-model.md). No new table, column, or migration.

**Testing**: This feature introduces no new Domain-layer business rules (data-model.md — no new
entities/invariants), so Constitution Principle III's mandatory-xUnit gate isn't triggered by new
Domain code the way it was for pricing/stock/void rules in `001-pos-system`. The new
Application-layer piece (`ExportSalesHistoryQuery`) is a thin pass-through to the repository, the
same shape as the already-un-unit-tested `SearchSalesOrdersQuery` it sits next to — consistent with
this codebase's actual precedent (`TaladPOS.Application.Tests` has never held real tests; every
use-case/query's correctness is instead verified through `TaladPOS.Api.IntegrationTests`, same as
`ReportsTests.cs` already does for the existing `/reports/*` endpoints). Given that, the concrete,
required tests for this feature are **API integration tests** (`TaladPOS.Api.IntegrationTests`)
covering: both endpoints return the right `Content-Type`/non-empty body for an Admin; a
`Cashier`-role token gets `403`; an empty result still returns `200` with a header-only file
(FR-009); and — the one behavior worth pinning down precisely — a Sale History export with more
matching orders than one page size returns *more rows than one page* (FR-004), not just whatever
`GET /sales-orders` would paginate to.

**Target Platform**: Unchanged — same self-hosted single-store web app (`api/` + PostgreSQL +
`web/`) from `001-pos-system`.

**Project Type**: Web application (frontend + backend), extending the existing `api/` + `web/`
split — no new top-level project.

**Performance Goals**: SC-004 — exporting up to one year of sale history completes within 10
seconds of clicking Export.

**Constraints**: Export endpoints must stream back a `Content-Disposition: attachment` file
response, not JSON (contracts/README.md); building the workbook must not require loading data the
UI wouldn't otherwise load for the equivalent on-screen view at single-store scale (research.md
items 5–6), so no new caching or background-job infrastructure is needed. The frontend must attach
the same JWT bearer token used by every other request (research.md item 3) — a plain browser
navigation to the export URL will not work.

**Scale/Scope**: Same single-store scale as `001-pos-system` (hundreds to low-thousands of SKUs;
sales history retained indefinitely, but each export call is bounded by whatever date filter the
Admin has set — up to ~1 year per SC-004).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. Clear API/Frontend Separation | `web/` triggers both exports via authenticated `fetch()` calls to the two new `api/` endpoints and never touches PostgreSQL directly; the new blob-download helper (research.md item 3) is still a REST/JSON-adjacent HTTP call, just with a binary response body instead of JSON | PASS |
| II. Domain-Driven Design for the API | ClosedXML is confined to `TaladPOS.Infrastructure` behind a new `IWorkbookExportService` interface declared in `TaladPOS.Application` (research.md item 2) — mirrors the existing `IProductImageStore` seam; `TaladPOS.Domain` gains nothing new and stays free of any export-specific or third-party types | PASS |
| III. Test-First for Business Logic | No new Domain-layer business rules are introduced (data-model.md); the new Application-layer query is a thin pass-through, matching this codebase's existing convention of verifying such queries via `TaladPOS.Api.IntegrationTests` rather than dedicated Application unit tests (Technical Context "Testing" above) | PASS |
| IV. Frontend Technology Standards | Still Next.js + Tailwind CSS; the only frontend addition is a small typed client function per export plus a shared `downloadBlob` DOM helper — no new UI framework, no direct database access | PASS |
| V. Repository & Folder Structure | All new files land inside the existing `api/` and `web/` trees (see Project Structure below); no new top-level source directory is introduced | PASS |

No violations after Phase 1 design either — Complexity Tracking is not applicable (left empty
below).

## Project Structure

### Documentation (this feature)

```text
specs/002-export-reports-sales-history/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   ├── README.md
│   ├── stock-export.md
│   └── sales-history-export.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
api/
├── Directory.Packages.props            # + ClosedXML package version entry
└── src/
    ├── TaladPOS.Application/
    │   ├── Reports/
    │   │   └── IWorkbookExportService.cs   # NEW — generic (sheetName, headers, rows) -> byte[]
    │   └── SalesOrders/
    │       └── ExportSalesHistoryQuery.cs  # NEW — no-paging search + staff/member name resolution
    ├── TaladPOS.Infrastructure/
    │   ├── Reports/
    │   │   └── ClosedXmlWorkbookExportService.cs  # NEW — implements IWorkbookExportService
    │   └── Persistence/Repositories/
    │       └── SalesOrderRepository.cs      # + SearchAllForExportAsync method
    └── TaladPOS.Api/Controllers/
        ├── Reports/ReportsController.cs         # + GET stock-levels/export action
        └── SalesOrders/SalesOrdersController.cs # + GET export action

web/
└── src/
    ├── lib/
    │   ├── api/
    │   │   ├── client.ts        # + apiFetchBlob helper (research.md item 3)
    │   │   ├── reports.ts       # + exportStockLevels(token)
    │   │   └── salesOrders.ts   # + exportSalesHistory(token, filters)
    │   └── download.ts          # NEW — downloadBlob(blob, filename) DOM helper
    └── app/(app)/
        ├── reports/page.tsx         # + Export button on the Stock levels section
        └── sales-history/page.tsx   # + Export button next to the existing filter controls
```

**Structure Decision**: Purely additive within the existing `api/` (ASP.NET Core, DDD-layered) and
`web/` (Next.js + Tailwind CSS) split from `001-pos-system` — no new top-level source directory, no
new project/solution file, consistent with Constitution Principles I, II, IV, and V.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
