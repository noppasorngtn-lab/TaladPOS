# Implementation Plan: ระบบ POS สำหรับร้านขายผลไม้และสินค้าทั่วไป (Single Store)

**Branch**: `001-pos-system` | **Date**: 2026-09-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-pos-system/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Build a single-store POS web application: cashiers sell products from an image-based catalog
(search by name/barcode, cart, checkout) with automatic stock deduction, membership lookup, and
promotion/member discounts; admins manage the product catalog, promotions, and view sales
history and reports (daily/monthly sales, best-sellers, per-staff sales, stock levels). Per the
project constitution, this is delivered as two independently deployable applications — a DDD-
structured ASP.NET Core Web API (`api/`) backed by PostgreSQL via EF Core, and a Next.js +
Tailwind CSS frontend (`web/`) that talks to the API exclusively over REST/JSON, using PrimeReact
(unstyled, Tailwind-themed via `tailwindcss-primeui`) for data-heavy UI components such as
`DataTable`, `Dialog`, and `Button`. Business logic
(pricing/discount calculation, stock guards, void rules) lives in the API's Domain layer with
mandatory unit test coverage.

## Technical Context

**Language/Version**: Backend: C# 12 / .NET 8 (LTS). Frontend: TypeScript 5 / Node.js 20 LTS.

**Primary Dependencies**: Backend: ASP.NET Core Web API, Entity Framework Core 8,
`Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.AspNetCore.Authentication.JwtBearer`, xUnit +
Moq + FluentAssertions (test-only). Frontend: Next.js 14 (App Router), Tailwind CSS, PrimeReact
(unstyled mode) + `tailwindcss-primeui` for `DataTable`/`Dialog`/`Button`/form components
(research.md item 9), a thin typed REST client (`fetch`-based) — no state-management library
required at this scope.

**Storage**: PostgreSQL 16, accessed only from `api/` via EF Core code-first migrations. Product
images stored as files under the API's static file directory (research.md item 3); no other
external storage.

**Testing**: xUnit unit tests for the Domain and Application layers are **mandatory**
(Constitution Principle III, NON-NEGOTIABLE) — pricing/discount calculation, stock guard, void
rules, member/barcode uniqueness rules. API integration tests are optional/recommended for
contract validation. The frontend has no test framework mandated by the constitution; correctness
is validated via the `quickstart.md` walkthrough.

**Target Platform**: Self-hosted web application for a single physical store — `api/` +
PostgreSQL run on an in-store server/PC or a small reachable VM; `web/` is served to a browser on
POS terminals/tablets (cashiers) and a desktop (admin). No native/offline app packaging; per
FR-006 the sales screen requires a live connection to the API at all times.

**Project Type**: Web application (frontend + backend), matching Constitution Principle V's
mandated `api/` + `web/` split.

**Performance Goals**: 95% of product searches return in <1s (SC-003); a 5-item checkout
completes in <1 minute end-to-end (SC-001); report and stock-level reads reflect the latest
committed transactions with no caching/staleness window, since volume at single-store scale does
not require one (research.md item 6).

**Constraints**: No offline mode — any lost connection during checkout MUST abort the whole
transaction, never a partial one (FR-006, SC-007). All `web/`↔`api/` communication MUST be
REST/JSON (Constitution Principle I). Stock decrement and void-restore MUST be transactional and
concurrency-safe to prevent overselling (research.md item 4). Discount calculation MUST live in
the API's Domain layer, not the frontend (research.md item 5), so unit tests are the source of
truth for correctness (Constitution Principle III).

**Scale/Scope**: Single store; a handful of concurrent POS terminals/cashiers; catalog on the
order of hundreds to low thousands of SKUs; sales history retained indefinitely (spec.md
Assumptions — no purge in this version).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. Clear API/Frontend Separation | `web/` calls `api/` only via the REST contracts in `contracts/`; no DB driver/connection string in `web/` (data-model.md, contracts/README.md) | PASS |
| II. Domain-Driven Design for the API | `api/` is planned as a multi-project solution (`Domain`/`Application`/`Infrastructure`/`Api`) with `Domain` free of EF Core/ASP.NET Core references; PostgreSQL + EF Core as mandated (research.md item 1) | PASS |
| III. Test-First for Business Logic | Pricing/discount calculation, stock guard, void rules, and uniqueness rules are identified as Domain/Application logic requiring xUnit coverage before merge (Technical Context "Testing"; research.md items 4–5) | PASS |
| IV. Frontend Technology Standards | `web/` is Next.js + Tailwind CSS, reads/writes only via the REST client in research.md item 8; no DB access. PrimeReact is used in **unstyled** mode with `tailwindcss-primeui` so Tailwind remains the sole visual styling system (research.md item 9) — component library, not a framework change | PASS |
| V. Repository & Folder Structure | Project Structure below places the entire .NET solution under `api/` and the entire Next.js app under `web/`, with no cross-imports | PASS |

No violations — Complexity Tracking is not applicable (left empty below).

## Project Structure

### Documentation (this feature)

```text
specs/001-pos-system/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   ├── README.md
│   ├── auth.md
│   ├── products.md
│   ├── members.md
│   ├── promotions.md
│   ├── sales-orders.md
│   └── reports.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
api/
├── src/
│   ├── TaladPOS.Domain/            # Entities, value objects, domain services (pricing,
│   │                                #   stock guard, void rules) — no EF Core/ASP.NET Core refs
│   ├── TaladPOS.Application/       # Use cases (checkout, void, member signup, reports queries),
│   │                                #   DTOs, interfaces (e.g., IProductImageStore)
│   ├── TaladPOS.Infrastructure/    # EF Core DbContext, PostgreSQL migrations, repositories,
│   │                                #   static-file product image store, JWT signing
│   └── TaladPOS.Api/               # Controllers/endpoints per contracts/*.md, auth wiring, DI
└── tests/
    ├── TaladPOS.Domain.Tests/          # Unit tests (NON-NEGOTIABLE) — pricing, stock, void
    ├── TaladPOS.Application.Tests/     # Unit tests for use-case orchestration
    └── TaladPOS.Api.IntegrationTests/  # Optional contract-level tests against contracts/*.md

web/
├── src/
│   ├── app/               # Next.js App Router routes: /login, /sales, /stock, /members,
│   │                       #   /promotions, /reports, /sales-history
│   ├── components/        # ProductCard, Cart, MemberSearch, PromotionForm, ReportTable —
│   │                       #   built on PrimeReact DataTable/Dialog/Button (unstyled mode)
│   ├── lib/                # Typed REST client wrapping contracts/*.md endpoints
│   └── styles/             # Tailwind config/globals + tailwindcss-primeui plugin setup
└── tests/                  # Optional; primary validation is quickstart.md
```

**Structure Decision**: Web application split into `api/` (ASP.NET Core, DDD-layered, PostgreSQL
via EF Core) and `web/` (Next.js + Tailwind CSS), per Constitution Principles I, II, IV, and V.
No other top-level source directories are introduced.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
