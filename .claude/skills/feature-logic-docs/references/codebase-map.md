# TaladPOS codebase map

Concrete folder paths and conventions for tracing a feature through the stack. This is a map, not
a substitute for reading the actual files — use it to go straight to the right place instead of
grepping blind.

## Two apps, one contract

- `web/` — Next.js 14 (App Router), Tailwind, PrimeReact (unstyled components, Tailwind-themed).
  Talks to `api/` only over REST/JSON, never touches the database directly.
- `api/` — ASP.NET Core Web API, DDD-layered, backed by PostgreSQL 16 via EF Core.
- `specs/001-pos-system/` — the design docs the code was built from: `spec.md` (functional
  requirements, cited in code as `FR-XXX`), `plan.md`, `data-model.md`, `research.md`, and
  `contracts/*.md` (one file per resource, e.g. `contracts/sales-orders.md`). These are useful
  leads but can drift from the code — always verify against the actual implementation before
  citing a behavior from a contract doc.

## Frontend (`web/src/`)

| What | Where | Notes |
|---|---|---|
| Routes | `app/login/`, `app/(app)/<route>/page.tsx` | Everything under the `(app)` route group requires auth (see `app/(app)/layout.tsx`) |
| Shared UI pieces | `components/*.tsx` | One component per file, named for what it renders (e.g. `Cart.tsx`, `ProductCard.tsx`) |
| Typed API clients | `lib/api/*.ts` (one file per backend resource: `products.ts`, `salesOrders.ts`, `members.ts`, `promotions.ts`, `auth.ts`) | Each exported function = one endpoint call; comments above each function usually cite the matching `contracts/*.md` file and FR number |
| Fetch plumbing | `lib/api/client.ts` | `apiFetch<T>()` — attaches the JWT bearer token, parses `{ error: { code, message } }` into `ApiError` |
| Auth/session state | `lib/auth/AuthProvider.tsx` | `token` + `staff` (with `role: "Cashier" | "Admin"`) held in React context, persisted to `localStorage` |

Convention: **the frontend never re-implements a calculation the backend already owns.** If you
see a `fallback*` value computed client-side (e.g. a naive subtotal), it's only used while waiting
for the real server response, not treated as authoritative — say so in the doc if you spot one.

## Backend (`api/src/`)

DDD layering, dependencies point inward (Api → Application → Domain ← Infrastructure):

| Layer | Project | What lives here |
|---|---|---|
| Api | `TaladPOS.Api/Controllers/<Resource>/` | `*Controller.cs` (HTTP routing, `[Authorize]`/`[Authorize(Policy = "Admin")]`, DTO mapping) + `*Dtos.cs` (request/response records) |
| Application | `TaladPOS.Application/<Resource>/` | Use cases (`VerbNounUseCase.cs`, e.g. `CheckoutUseCase`) and queries (`VerbNounQuery.cs`, e.g. `PricingPreviewQuery`) — orchestrate repositories + domain calls, no business rules of their own |
| Domain | `TaladPOS.Domain/Entities/*.cs` | Aggregate roots and entities with the actual business rules as methods (e.g. `Product.DecreaseStock()`, `SalesOrder.Void()`) — private setters, validation in constructors/methods, not anemic DTOs |
| Domain (shared logic) | `TaladPOS.Domain/<Concept>/*Service.cs` | Cross-cutting calculations used by more than one use case — e.g. `Pricing/SalesOrderPricingService.cs` is called by both checkout and the live pricing preview so they can never disagree. If a feature has one of these, it's the single most important file to read fully. |
| Domain | `TaladPOS.Domain/Exceptions/*.cs` | Named exceptions (e.g. `InsufficientStockException`, `AlreadyVoidedException`) that controllers/middleware translate to specific HTTP status codes |
| Infrastructure | `TaladPOS.Infrastructure/Persistence/Repositories/*Repository.cs` | EF Core LINQ queries — the ground truth for which tables/columns are actually touched and how (filters, includes, ordering) |
| Infrastructure | `TaladPOS.Infrastructure/Persistence/TaladPOSDbContext.cs` | `DbSet<T>` properties = table names; check here if a repository's queries aren't enough to confirm a table name |

Naming convention: interfaces for repositories live in `Application/<Resource>/I<Resource>Repository.cs`;
implementations live in `Infrastructure/Persistence/Repositories/<Resource>Repository.cs`.

## Auth model (relevant to almost every feature)

- Login (`POST /auth/login`, not detailed here) returns a JWT + `StaffSummary` (`role`).
- Every controller defaults to `[Authorize]`; admin-only endpoints add
  `[Authorize(Policy = "Admin")]`. `StaffRole` enum: `Cashier`, `Admin` (Admin is a superset).
- The acting staff member's id comes from the JWT claim via `User.GetStaffId()` server-side —
  never trust a `staffId` field sent from the client for "who did this."

## Database access without a running instance

You almost never need a live database to document a feature — the EF Core entity classes
(`Domain/Entities/*.cs`) define every column, and repository LINQ queries show exactly which
columns are filtered/read/written. Reading those two things is normally sufficient for an accurate
data-model doc.

If you want to double-check against a live database anyway (optional):

```bash
docker ps --filter "name=taladpos-postgres"        # confirm the dev container is up
docker exec taladpos-postgres psql -U postgres -d taladpos -c "\dt"          # list tables
docker exec taladpos-postgres psql -U postgres -d taladpos -c 'SELECT * FROM "TableName" LIMIT 5;'
```

Connection details (dev): see `api/src/TaladPOS.Api/appsettings.Development.json` →
`ConnectionStrings:DefaultConnection` (currently `localhost:5433`, database `taladpos`).

## Known project vocabulary worth reusing in docs

- **Sequential discounting** — when a feature applies more than one discount, later discounts
  apply to the amount remaining after earlier ones, not to the original total. Confirmed pattern
  in `SalesOrderPricingService`; check whether a new feature you're documenting follows the same
  rule before assuming it does.
- **Snapshot fields** — line items capture the name/price *at the time of the transaction*
  (e.g. `ProductNameSnapshot`, `UnitPriceSnapshot` on `SalesOrderLine`) rather than joining live to
  the current product row. This is deliberate (historical accuracy), not an oversight — call it out
  when you see the pattern rather than describing it as denormalization.
- **Soft delete / deactivate** — `IsActive` flags (`Product.Deactivate()`, `Promotion.Deactivate()`)
  instead of hard deletes, so historical records that reference the row stay valid.
