# Research: ระบบ POS สำหรับร้านขายผลไม้และสินค้าทั่วไป (Single Store)

**Feature**: `001-pos-system` | **Date**: 2026-09-10

**Input constraints**: The project constitution (`.specify/memory/constitution.md`) already fixes
the technology stack (ASP.NET Core Web API + EF Core + PostgreSQL + DDD for `api/`; Next.js +
Tailwind CSS + REST-only for `web/`), so no `NEEDS CLARIFICATION` markers remain in the Technical
Context. This research resolves the remaining implementation-approach decisions needed before
design (Phase 1).

## 1. .NET Solution Layout for DDD Enforcement

- **Decision**: Multi-project .NET solution — `TaladPOS.Domain`, `TaladPOS.Application`,
  `TaladPOS.Infrastructure`, `TaladPOS.Api` — with project references flowing inward only
  (`Api` → `Application` → `Domain`; `Infrastructure` → `Domain`/`Application`). `Domain` has zero
  package references to EF Core or ASP.NET Core.
- **Rationale**: Compiler-enforced project references make Constitution Principle II
  (DDD layering, domain independent of persistence/transport) impossible to violate accidentally,
  and give the Domain/Application test projects (Principle III) a natural, dependency-free target.
- **Alternatives considered**: Single project with folder-based layering — rejected because
  nothing stops a domain class from `using Microsoft.EntityFrameworkCore` by accident; only a
  code-review catches it, not the compiler.

## 2. Authentication for Staff Login

- **Decision**: JWT bearer tokens issued by the API after username/password validation
  (`FR-007`), stored client-side by the Next.js app and sent as `Authorization: Bearer` on every
  REST call. Roles (`cashier`, `admin`) embedded as a claim (`FR-034`).
- **Rationale**: Fits the REST-only, stateless boundary in Constitution Principle I — the API
  never needs server-side session state, and the frontend never touches the database to check
  who's logged in. Standard ASP.NET Core support (`Microsoft.AspNetCore.Authentication.JwtBearer`).
- **Alternatives considered**: Cookie-based session — rejected, it couples the API to
  server-side session storage and complicates a future move to multiple API instances; not needed
  for a single-store deployment but JWT costs nothing extra and keeps the option open.

## 3. Product Image Storage

- **Decision**: Store uploaded product images as files under the API's static file directory
  (`wwwroot/product-images/`), with the relative URL saved on the `Product` entity.
- **Rationale**: Single-store scale (hundreds to low thousands of SKUs) does not justify an
  external object-storage dependency; serving static files directly from the API keeps the stack
  within what the constitution already mandates (no new infrastructure to approve).
- **Alternatives considered**: Cloud blob storage (e.g., S3-compatible) — rejected as
  over-engineering for this scope; can be swapped in later behind the same `IProductImageStore`
  interface in `Infrastructure` without touching `Domain`/`Application`.

## 4. Preventing Overselling Under Concurrent Checkouts

- **Decision**: Stock decrement happens inside the same database transaction as sales-order
  creation, using EF Core's optimistic concurrency token (`xmin`/`RowVersion`) on `Product.
  QuantityOnHand`. If a concurrent checkout already dropped the quantity below what the current
  request needs, the transaction fails and the API returns a 409 Conflict, which the Domain
  layer surfaces as an `InsufficientStockException` (covers the edge case in spec.md and FR-005).
- **Rationale**: Matches PostgreSQL/EF Core idioms already in the stack; avoids introducing a
  distributed lock or queue for a single-store, low-concurrency scenario.
- **Alternatives considered**: Pessimistic row locking (`SELECT ... FOR UPDATE`) — viable but
  unnecessary complexity at this scale; optimistic concurrency is simpler to unit-test in the
  Domain layer per Principle III.

## 5. Combined Discount Calculation (Promotion + Member)

- **Decision**: A `Domain` pricing service (`SalesOrderPricingService` or equivalent) computes
  line and order totals: apply per-product/whole-bill promotions first, then apply the member
  discount percentage (if any) to the remaining amount, clamped so the net total never drops
  below zero — implementing FR-036 as a pure, dependency-free Domain function.
- **Rationale**: Keeping this calculation in the Domain layer (not in a controller or the
  frontend) makes it directly unit-testable per Principle III and guarantees the same math runs
  regardless of which client calls the API.
- **Alternatives considered**: Computing discounts in the frontend for instant UI feedback —
  rejected as the source of truth; the API MUST own pricing so the frontend cannot diverge from
  billed amounts. The frontend may still show a live preview by calling a pricing preview
  endpoint, not by re-implementing the math.

## 6. Reporting Approach

- **Decision**: Reports (daily/monthly sales, best-sellers, per-staff sales, stock levels) are
  served by read-oriented Application-layer queries running directly against the PostgreSQL
  operational data (no separate reporting database or nightly ETL), excluding voided orders per
  FR-033.
- **Rationale**: Single-store transaction volume is small enough that ad-hoc aggregate queries
  (`GROUP BY` day/month/staff/product) meet the SC-004 accuracy target without added
  infrastructure.
- **Alternatives considered**: A separate data warehouse / OLAP store — rejected as unjustified
  complexity for this scale; can be revisited if the store later adds branches.

## 7. Low-Stock Notification Delivery

- **Decision**: Surfaced as an in-app indicator on the Stock Management screen (badge/list of
  products at or below their threshold), computed on read — no push notification, email, or SMS
  channel in this version.
- **Rationale**: No requirement or constitution mandate for an external notification channel;
  an in-app read satisfies FR-013/SC-006 without adding a messaging dependency.
- **Alternatives considered**: Email/SMS alerts — deferred; out of scope per spec Assumptions
  (single store, admin is expected to check the dashboard regularly).

## 8. Frontend Data Fetching Pattern

- **Decision**: Next.js App Router with server components for initial reads (server-side fetch
  to the API using the user's forwarded JWT) and client components for interactive flows (cart,
  search-as-you-type, live cart totals), calling the API via a thin typed REST client in
  `web/src/lib/api`.
- **Rationale**: Matches Constitution Principle IV (REST-only, no DB access from `web/`) while
  using Next.js idiomatically; keeps secrets (JWT) server-side where possible and avoids
  duplicating pricing logic in the browser (see research item 5).
- **Alternatives considered**: Fully client-rendered SPA calling the API from the browser only —
  simpler, still acceptable; either approach fits the constitution — the choice is a Phase 1
  implementation detail, not a constitutional gate, and can be adjusted per screen during
  `/speckit-tasks`.

## 9. UI Component Library for the Frontend

- **Decision**: Use PrimeReact (https://primereact.dev/) for structural/interactive components —
  `DataTable` (sales history, reports, product/promotion lists), `Dialog` (add/edit product,
  add/edit promotion, member signup), `Button`, form inputs, etc. — mounted in **unstyled mode**
  with the official `tailwindcss-primeui` Tailwind plugin providing the visual styling, instead
  of PrimeReact's own bundled theme CSS.
- **Rationale**: Constitution Principle IV mandates the frontend be "styled with Tailwind CSS."
  PrimeReact's default themed build ships its own CSS design system, which would compete with
  Tailwind as the source of visual styling. PrimeReact's unstyled mode (`unstyled` prop /
  `PrimeReactProvider` config) strips that default CSS and exposes pass-through (`pt`) hooks so
  every component's markup can be styled entirely with Tailwind utility classes — this satisfies
  the user's request to reuse PrimeReact's component behavior (DataTable sorting/pagination,
  Dialog focus-trapping, accessible Button semantics) for the data-heavy admin screens (User
  Stories 2, 4, 5) without violating the constitution's styling mandate. This does not introduce
  a new frontend *framework* (Next.js is unchanged), so no constitution amendment is required —
  only Principle IV's Technology Stack list gains one additional dependency.
- **Alternatives considered**: PrimeReact's default themed CSS (e.g., Lara/Aura theme) — rejected
  as a direct conflict with Principle IV. Headless component libraries built for Tailwind from
  the start (e.g., Radix UI + shadcn/ui) — a reasonable alternative, but rejected here because
  the user explicitly asked for PrimeReact by name for `DataTable`/`Dialog`/`Button`.

## Outstanding Items

None — all Technical Context fields are resolved; Phase 1 design may proceed.
