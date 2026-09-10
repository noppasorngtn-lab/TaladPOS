---

description: "Task list template for feature implementation"
---

# Tasks: ระบบ POS สำหรับร้านขายผลไม้และสินค้าทั่วไป (Single Store)

**Input**: Design documents from `/specs/001-pos-system/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md (all present)

**Tests**: Constitution Principle III (NON-NEGOTIABLE) requires unit test coverage for all
Domain/Application business logic. Test tasks below cover that mandate (stock guard, void rules,
uniqueness rules, discount calculation) — they are not optional for this project. No test tasks
are included for `web/`, which the constitution does not mandate testing for; `web/` correctness
is validated via `quickstart.md`.

**Organization**: Tasks are grouped by user story (US1–US5, matching spec.md priorities P1–P5) to
enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: Which user story this task belongs to (US1–US5)
- Paths follow plan.md's Project Structure: `api/src/TaladPOS.{Domain,Application,Infrastructure,Api}/...`, `api/tests/...`, `web/src/...`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Repository scaffolding and toolchain initialization for both `api/` and `web/`

- [ ] T001 Create top-level repository structure: `api/` and `web/` directories with the subfolders listed in plan.md's Project Structure (no code yet)
- [ ] T002 Initialize .NET 8 solution `api/TaladPOS.sln` with four src projects (`TaladPOS.Domain`, `TaladPOS.Application`, `TaladPOS.Infrastructure`, `TaladPOS.Api`) and three test projects (`TaladPOS.Domain.Tests`, `TaladPOS.Application.Tests`, `TaladPOS.Api.IntegrationTests`), wired with inward-only project references (`Api`→`Application`→`Domain`; `Infrastructure`→`Domain`,`Application`) so `TaladPOS.Domain` has **zero** package or project references to EF Core or ASP.NET Core (Constitution Principle II; research.md item 1)
- [ ] T003 [P] Add NuGet packages per plan.md Technical Context: `Microsoft.EntityFrameworkCore` + `Npgsql.EntityFrameworkCore.PostgreSQL` to `TaladPOS.Infrastructure`, `Microsoft.AspNetCore.Authentication.JwtBearer` to `TaladPOS.Api`, `xunit` + `Moq` + `FluentAssertions` to all three test projects
- [ ] T004 [P] Initialize Next.js 14 (App Router, TypeScript) project in `web/` with Tailwind CSS configured
- [ ] T005 [P] Add PrimeReact and the `tailwindcss-primeui` plugin to `web/`; configure `PrimeReactProvider` in **unstyled** mode in `web/src/app/layout.tsx` and register the plugin in `web/tailwind.config.ts` so Tailwind remains the sole visual styling system (Constitution Principle IV; research.md item 9)
- [ ] T006 [P] Configure linting/formatting: `.editorconfig` + `dotnet format` check for `api/`, ESLint + Prettier for `web/`

**Checkpoint**: Both `api/` and `web/` build/run empty shells; solution/project structure matches plan.md.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented — data model, auth, and base app shells that every screen depends on (every screen requires a logged-in staff member per FR-007).

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T007 [P] Create `Product` entity in `api/src/TaladPOS.Domain/Entities/Product.cs` per data-model.md: `Price > 0`, `QuantityOnHand >= 0`, `Barcode` optional but unique when present, `IsActive` soft-delete flag, `RowVersion` concurrency token
- [ ] T008 [P] Create `Staff` entity in `api/src/TaladPOS.Domain/Entities/Staff.cs` per data-model.md: `Username` unique, `PasswordHash`, `Role` enum (`Cashier`, `Admin`), `IsActive`
- [ ] T009 [P] Create `Member` entity in `api/src/TaladPOS.Domain/Entities/Member.cs` per data-model.md: `PhoneNumber` unique, `AccumulatedPurchaseTotal >= 0` starting at 0, `JoinedAt`
- [ ] T010 [P] Create `Promotion` entity in `api/src/TaladPOS.Domain/Entities/Promotion.cs` per data-model.md: `Scope` enum (`PerProduct`, `WholeBill`, `MemberDiscount`), `ProductId` required iff `Scope = PerProduct`, `0 < DiscountPercent <= 100`, `EndDate >= StartDate`
- [ ] T011 [P] Create `SalesOrder` entity in `api/src/TaladPOS.Domain/Entities/SalesOrder.cs` per data-model.md: `Status` enum (`Completed`, `Voided`), `NetTotal >= 0`, `VoidedAt` nullable, one-way `Completed → Voided` transition field
- [ ] T012 [P] Create `SalesOrderLine` entity in `api/src/TaladPOS.Domain/Entities/SalesOrderLine.cs` per data-model.md: `Quantity > 0`, immutable `ProductNameSnapshot`/`UnitPriceSnapshot` captured at sale time
- [ ] T013 Create `TaladPOSDbContext` in `api/src/TaladPOS.Infrastructure/Persistence/TaladPOSDbContext.cs` mapping all six entities from T007–T012, with unique indexes on `Product.Barcode`, `Staff.Username`, `Member.PhoneNumber`, and the `RowVersion` concurrency token on `Product` (depends on T007–T012)
- [ ] T014 Create the initial EF Core migration and configure the PostgreSQL connection string (Npgsql) and DI registration for `TaladPOSDbContext` in `api/src/TaladPOS.Api/Program.cs` and `api/src/TaladPOS.Api/appsettings.Development.json` (depends on T013)
- [ ] T015 Implement JWT infrastructure — password hashing and token issuance/validation with a `Role` claim — in `api/src/TaladPOS.Infrastructure/Auth/JwtTokenService.cs` and `PasswordHasher.cs` (depends on T008; research.md item 2)
- [ ] T016 Implement `POST /auth/login`, `POST /auth/logout`, `GET /auth/me` in `api/src/TaladPOS.Api/Controllers/AuthController.cs` per contracts/auth.md (depends on T015)
- [ ] T017 Wire JWT bearer authentication middleware and `Cashier`/`Admin` authorization policies in `api/src/TaladPOS.Api/Program.cs` (depends on T015)
- [ ] T018 Implement global error-handling middleware in `api/src/TaladPOS.Api/Middleware/ErrorHandlingMiddleware.cs` producing the `{ "error": { "code", "message" } }` shape from contracts/README.md, mapping domain exceptions to `409 Conflict` (concurrency/state conflicts) and `422 Unprocessable Entity` (validation failures)
- [ ] T019 Add an EF Core migration seed that creates one initial `Admin` staff account, required to bootstrap the very first login per quickstart.md prerequisites, in `api/src/TaladPOS.Infrastructure/Persistence/Seed/AdminSeed.cs` (depends on T014, T015)
- [ ] T020 [P] Implement the typed REST client base (fetch wrapper attaching the JWT `Authorization` header, parsing the `{ error }` shape from T018) in `web/src/lib/api/client.ts`
- [ ] T021 [P] Implement auth context/provider and the `/login` page (calls `POST /auth/login`, stores the JWT, redirects by role) in `web/src/app/login/page.tsx` and `web/src/lib/auth/AuthProvider.tsx` (depends on T020, T016)
- [ ] T022 [P] Implement a shared authenticated-layout route guard (redirects unauthenticated users to `/login`) in `web/src/app/(app)/layout.tsx` (depends on T021)

**Checkpoint**: Database schema exists, staff can log in and receive a role-scoped JWT, and both apps have a base authenticated shell — user story implementation can now begin.

---

## Phase 3: User Story 1 - ขายสินค้าหน้าร้าน (Priority: P1) 🎯 MVP

**Goal**: A logged-in cashier can search/browse products, build a cart, and check out; stock is
cut automatically and the bill is attributed to the cashier; an admin can void a same-day bill.

**Independent Test**: Seed one product directly in the database, log in as a cashier, search it
by name and by barcode, add/adjust/remove it in the cart, check out, and confirm the stock
decreases and a `SalesOrder` is recorded with the cashier's id — all without any other story's
UI existing yet.

### Tests for User Story 1 (Constitution Principle III — NON-NEGOTIABLE for business logic)

> Write these tests FIRST; confirm they FAIL before implementing T025–T026.

- [ ] T023 [P] [US1] Unit tests for the stock guard in `api/tests/TaladPOS.Domain.Tests/StockGuardTests.cs`: a line quantity greater than `Product.QuantityOnHand` is rejected (FR-005); a line with `Quantity <= 0` is rejected (edge case in spec.md)
- [ ] T024 [P] [US1] Unit tests for `SalesOrder` void rules in `api/tests/TaladPOS.Domain.Tests/SalesOrderVoidTests.cs`: voiding is allowed only when `CreatedAt` is the same calendar day (FR-027); voiding an already-`Voided` order is rejected (FR-028); a successful void restores every line's quantity to `Product.QuantityOnHand`

### Implementation for User Story 1

- [ ] T025 [US1] Implement `SalesOrder` creation on the aggregate (add lines, compute `SubtotalAmount`/`NetTotal`, decrement each `Product.QuantityOnHand` via its `RowVersion` optimistic-concurrency token, raise `InsufficientStockException` on conflict) in `api/src/TaladPOS.Domain/Entities/SalesOrder.cs` (depends on T023 failing, T007, T011, T012; research.md item 4)
- [ ] T026 [US1] Implement the `Completed → Voided` transition (same-day check, idempotency guard, per-line stock restore) in `api/src/TaladPOS.Domain/Entities/SalesOrder.cs` (depends on T024 failing, T025)
- [ ] T027 [US1] Implement `CheckoutUseCase` (validate lines, load products, invoke T025, persist transactionally) in `api/src/TaladPOS.Application/SalesOrders/CheckoutUseCase.cs` (depends on T025)
- [ ] T028 [US1] Implement `VoidSalesOrderUseCase` in `api/src/TaladPOS.Application/SalesOrders/VoidSalesOrderUseCase.cs` (depends on T026)
- [ ] T029 [US1] Implement `SearchProductsQuery` matching by name-contains or exact barcode in `api/src/TaladPOS.Application/Products/SearchProductsQuery.cs` (FR-002)
- [ ] T030 [US1] Implement `POST /sales-orders`, `GET /sales-orders/{id}`, `POST /sales-orders/{id}/void`, `GET /sales-orders/pricing-preview` in `api/src/TaladPOS.Api/Controllers/SalesOrdersController.cs` per contracts/sales-orders.md (depends on T027, T028)
- [ ] T031 [US1] Implement `GET /products` (search-only; full CRUD is US2) in `api/src/TaladPOS.Api/Controllers/ProductsController.cs` per contracts/products.md (depends on T029)
- [ ] T032 [US1] Implement the EF Core repository for `Product` lookup-by-barcode-or-name and `SalesOrder` persistence in `api/src/TaladPOS.Infrastructure/Persistence/Repositories/ProductRepository.cs` and `SalesOrderRepository.cs` (depends on T013)
- [ ] T033 [P] [US1] Sales screen product grid: image cards with name/price, search box wired to `GET /products` in `web/src/app/(app)/sales/page.tsx` and `web/src/components/ProductCard.tsx` (FR-001, FR-002)
- [ ] T034 [P] [US1] Cart component (add/increase/decrease/remove lines, live subtotal) using PrimeReact `Button` in `web/src/components/Cart.tsx` (FR-003, FR-004)
- [ ] T035 [US1] Checkout action: submit cart to `POST /sales-orders`; surface `409` insufficient-stock per line; treat any network failure as a full abort with no partial order shown (FR-005, FR-006, SC-007) in `web/src/app/(app)/sales/page.tsx` (depends on T033, T034, T030)
- [ ] T036 [US1] Sales route guard: `/sales` requires an authenticated session (any role) via the T022 layout guard (FR-007) in `web/src/app/(app)/sales/layout.tsx` (depends on T022)
- [ ] T037 [US1] Void action: Admin-only "Void" button with a PrimeReact `Dialog` confirmation calling `POST /sales-orders/{id}/void` in `web/src/components/VoidOrderDialog.tsx` (depends on T030)

**Checkpoint**: User Story 1 is fully functional and independently testable — a cashier can sell, an admin can void, stock stays correct.

---

## Phase 4: User Story 2 - จัดการสต็อกสินค้า (Priority: P2)

**Goal**: An admin can add/edit/soft-delete products with images, price, and quantity, and sees a
low-stock indicator.

**Independent Test**: As Admin, add a new product with an image/price/quantity, confirm it is
immediately searchable on the Sales screen (US1); edit its price and confirm history is
untouched; lower its quantity below its threshold and confirm it appears on the low-stock list —
all without creating any sale.

### Tests for User Story 2

- [ ] T038 [P] [US2] Unit tests for product validation rules in `api/tests/TaladPOS.Domain.Tests/ProductTests.cs`: `Price > 0` rejected otherwise, `QuantityOnHand >= 0` rejected otherwise, duplicate non-null `Barcode` rejected (FR-014)

### Implementation for User Story 2

- [ ] T039 [US2] Implement product create/edit/soft-delete in `api/src/TaladPOS.Application/Products/ManageProductUseCases.cs` (depends on T038 failing, T007)
- [ ] T040 [US2] Implement `POST /products`, `PUT /products/{id}`, `DELETE /products/{id}`, `GET /products/{id}`, `GET /products/low-stock` in `api/src/TaladPOS.Api/Controllers/ProductsController.cs` per contracts/products.md (depends on T039; extends T031's controller)
- [ ] T041 [US2] Implement static-file product image storage in `api/src/TaladPOS.Infrastructure/Storage/ProductImageStore.cs`, served from `wwwroot/product-images/` (research.md item 3)
- [ ] T042 [P] [US2] Stock management screen: PrimeReact `DataTable` listing products with a low-stock badge, sourced from `GET /products` and `GET /products/low-stock` in `web/src/app/(app)/stock/page.tsx` (FR-012, FR-013)
- [ ] T043 [P] [US2] Add/Edit product PrimeReact `Dialog` form (name, image upload, price, quantity, barcode, low-stock threshold) in `web/src/components/ProductFormDialog.tsx` (FR-009, FR-010)
- [ ] T044 [US2] Wire the soft-delete action from the stock DataTable to `DELETE /products/{id}` in `web/src/app/(app)/stock/page.tsx` (depends on T042; FR-011)

**Checkpoint**: User Stories 1 and 2 both work independently — stock managed here immediately shows up on the Sales screen.

---

## Phase 5: User Story 3 - ระบบสมาชิกและส่วนลดสมาชิก (Priority: P3)

**Goal**: A cashier can sign up a member and link a sale to an existing member by phone number to
accrue their purchase total and apply a member discount.

**Independent Test**: Sign up a member with a phone number and name; search that phone number
during a new checkout, complete the sale, and confirm the member's accumulated total increased by
the order's net amount — independent of promotions (US4) being configured.

### Tests for User Story 3

- [ ] T045 [P] [US3] Unit tests in `api/tests/TaladPOS.Domain.Tests/MemberTests.cs`: duplicate `PhoneNumber` signup rejected (FR-016); `AccumulatedPurchaseTotal` increases by a completed order's `NetTotal` and decreases by the same amount if that order is later voided

### Implementation for User Story 3

- [ ] T046 [US3] Implement member signup and phone lookup in `api/src/TaladPOS.Application/Members/ManageMemberUseCases.cs` (depends on T045 failing, T009)
- [ ] T047 [US3] Implement `POST /members`, `GET /members?phone=`, `GET /members/{id}` in `api/src/TaladPOS.Api/Controllers/MembersController.cs` per contracts/members.md (depends on T046)
- [ ] T048 [US3] Extend `CheckoutUseCase`/`VoidSalesOrderUseCase` (T027/T028) to credit `Member.AccumulatedPurchaseTotal` on checkout and reverse it on void when a `MemberId` is present, in `api/src/TaladPOS.Application/SalesOrders/CheckoutUseCase.cs` and `VoidSalesOrderUseCase.cs` (depends on T045 failing, T027, T028, T046)
- [ ] T049 [P] [US3] Member signup PrimeReact `Dialog` form in `web/src/components/MemberSignupDialog.tsx` (FR-015)
- [ ] T050 [US3] Member search-by-phone wired into the Sales screen cart (link/unlink member, show "not found → sign up or continue" per spec.md edge case) in `web/src/app/(app)/sales/page.tsx` (depends on T033, T047, T049; FR-017)

**Checkpoint**: User Stories 1–3 all work independently; membership accrual is correct with or without promotions active.

---

## Phase 6: User Story 4 - ระบบโปรโมชั่น (Priority: P4)

**Goal**: An admin can configure percentage discounts (per-product or whole-bill) with a date
range, plus a separate member-discount percentage, and the sales flow applies them automatically
and itemizes them.

**Independent Test**: Create a per-product promotion covering today's date, sell that product,
and confirm the discount is applied; create one with a future start date and confirm it is NOT
applied today — independent of any member being linked.

### Tests for User Story 4

- [ ] T051 [P] [US4] Unit tests in `api/tests/TaladPOS.Domain.Tests/PromotionPricingTests.cs`: `Promotion` validation (`EndDate >= StartDate`; `ProductId` required iff `Scope = PerProduct`; `0 < DiscountPercent <= 100`); combined discount calculation applies promotion first, then the member discount to the remainder, clamped so `NetTotal >= 0` (FR-036); a promotion outside `[StartDate, EndDate]` is not applied (FR-023)

### Implementation for User Story 4

- [ ] T052 [US4] Implement Promotion create/edit/deactivate in `api/src/TaladPOS.Application/Promotions/ManagePromotionUseCases.cs` (depends on T051 failing, T010)
- [ ] T053 [US4] Implement `SalesOrderPricingService` (sequential promotion-then-member discount calculation, FR-036) in `api/src/TaladPOS.Domain/Pricing/SalesOrderPricingService.cs`, and call it from `CheckoutUseCase` and the pricing-preview endpoint instead of the flat subtotal used by US1 (depends on T051 failing, T027)
- [ ] T054 [US4] Implement `POST /promotions`, `PUT /promotions/{id}`, `DELETE /promotions/{id}`, `GET /promotions`, `GET /promotions/applicable` in `api/src/TaladPOS.Api/Controllers/PromotionsController.cs` per contracts/promotions.md (depends on T052)
- [ ] T055 [P] [US4] Promotion management screen: PrimeReact `DataTable` + `Dialog` form (scope, product picker, discount %, date range) in `web/src/app/(app)/promotions/page.tsx` (FR-020–FR-022)
- [ ] T056 [US4] Update the Sales screen cart/checkout to show itemized promotion and member discount amounts from the pricing-preview response (FR-024) in `web/src/app/(app)/sales/page.tsx` (depends on T035, T053)

**Checkpoint**: User Stories 1–4 all work independently; discount math is unit-tested and identical between preview and checkout.

---

## Phase 7: User Story 5 - ประวัติการขายและรายงาน (Priority: P5)

**Goal**: An admin can search sale history and view daily/monthly sales, best-sellers,
per-staff sales, and current stock-level reports, all excluding voided orders.

**Independent Test**: Using sales data already created by US1–US4 testing, open each report and
confirm totals match the sum of the underlying non-voided sales orders.

### Implementation for User Story 5

- [ ] T057 [US5] Implement `SearchSalesOrdersQuery` (filters: date range, staff, member, status) in `api/src/TaladPOS.Application/SalesOrders/SearchSalesOrdersQuery.cs` (FR-026)
- [ ] T058 [US5] Implement report queries — sales-summary (daily/monthly), best-sellers, sales-by-staff, stock-levels — excluding `Voided` orders (FR-033) in `api/src/TaladPOS.Application/Reports/` (FR-029–FR-032)
- [ ] T059 [US5] Implement `GET /sales-orders` (history) in `api/src/TaladPOS.Api/Controllers/SalesOrdersController.cs`, and `GET /reports/sales-summary`, `GET /reports/best-sellers`, `GET /reports/sales-by-staff`, `GET /reports/stock-levels` in `api/src/TaladPOS.Api/Controllers/ReportsController.cs` per contracts/sales-orders.md and contracts/reports.md (depends on T057, T058)
- [ ] T060 [P] [US5] Sales history screen: PrimeReact `DataTable` with date/staff/member/status filters in `web/src/app/(app)/sales-history/page.tsx` (FR-026)
- [ ] T061 [P] [US5] Reports screen(s): one PrimeReact `DataTable` per report type in `web/src/app/(app)/reports/page.tsx` (FR-029–FR-032)

**Checkpoint**: All five user stories are independently functional and match spec.md's acceptance scenarios.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Validation and hardening that spans multiple user stories

- [ ] T062 Run the full `quickstart.md` validation walkthrough end-to-end against the deployed `api/` + `web/`
- [ ] T063 [P] Add API integration tests covering the contracts in `contracts/*.md` in `api/tests/TaladPOS.Api.IntegrationTests/`
- [ ] T064 Security hardening: verify Admin-only endpoints reject a `Cashier`-role JWT with `403`, and verify an expired/invalid JWT is rejected with `401`
- [ ] T065 [P] Update the repository root `README.md` with `api/`/`web/` setup and run instructions, referencing `quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phase 3–7)**: All depend on Foundational completion
  - US1 has no dependency on other stories
  - US2 has no dependency on other stories (its data is consumed by US1's Sales screen, but US2 itself is independently testable per its own Independent Test)
  - US3 extends US1's checkout/void use cases (T048) but is independently testable on its own signup/lookup flow
  - US4 extends US1's checkout pricing (T053, T056) but is independently testable on its own promotion CRUD + isolated pricing-calculation tests
  - US5 only reads data produced by US1–US4; no other story depends on it
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: Can start after Phase 2 — no dependency on other stories
- **US2 (P2)**: Can start after Phase 2 — no dependency on other stories
- **US3 (P3)**: Can start after Phase 2; T048/T050 touch files US1 created (T027, T028, T033) — implement after US1's corresponding tasks land
- **US4 (P4)**: Can start after Phase 2; T053/T056 touch files US1 created (T027, T035) — implement after US1's corresponding tasks land
- **US5 (P5)**: Can start after Phase 2; reads data shapes from all prior stories, so is most useful (and most testable end-to-end) once US1–US4 exist

### Within Each User Story

- Domain unit tests (where present) MUST be written and FAIL before their corresponding Domain implementation task
- Domain before Application (use cases) before Api (controllers) before web/ UI
- Story complete and checkpointed before moving to the next priority

### Parallel Opportunities

- T003, T004, T005, T006 (Setup) can run in parallel once their own prerequisite lands
- T007–T012 (all six Domain entities in Foundational) can run in parallel
- T020, T021, T022 (web/ foundational shell) can run in parallel with T015–T019 (api/ auth) since they touch disjoint file trees, though T021 needs T016 deployed to test against
- Within US1: T023/T024 (tests) in parallel; T033/T034 (UI) in parallel
- Within US2: T042/T043 (UI) in parallel
- Across stories: once Phase 2 is complete, US1 and US2 can be staffed and built in parallel by different developers; US3/US4 are best started once their US1 integration points (T027, T028, T033, T035) exist

---

## Parallel Example: User Story 1

```bash
# Launch both Domain test files together:
Task: "Unit tests for the stock guard in api/tests/TaladPOS.Domain.Tests/StockGuardTests.cs"
Task: "Unit tests for SalesOrder void rules in api/tests/TaladPOS.Domain.Tests/SalesOrderVoidTests.cs"

# Launch both UI components together (after Foundational is done):
Task: "Sales screen product grid in web/src/app/(app)/sales/page.tsx and web/src/components/ProductCard.tsx"
Task: "Cart component in web/src/components/Cart.tsx"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: run quickstart.md sections 1 and 3 (login + core sale + stock cut)
5. Deploy/demo if ready — this alone is a usable single-till cash register

### Incremental Delivery

1. Setup + Foundational → foundation ready (DB schema, login, base shells)
2. Add US1 → validate independently → demo (MVP!)
3. Add US2 → validate independently → demo (admin can now manage the catalog instead of seeding it by hand)
4. Add US3 → validate independently → demo (loyalty accrual)
5. Add US4 → validate independently → demo (discounts)
6. Add US5 → validate independently → demo (history + reports)
7. Phase 8 → polish, run full quickstart.md, close out

### Parallel Team Strategy

With multiple developers, once Phase 2 (Foundational) is done:

- Developer A: User Story 1 (core sales flow — the critical path for MVP)
- Developer B: User Story 2 (stock management — needed to populate real data for A's testing)
- Developer C: User Story 5 groundwork (report query shapes) once US1/US2 data exists, then US3/US4 as US1 integration points land

---

## Notes

- [P] tasks touch different files with no dependency on an incomplete task
- [Story] label maps each task to its user story for traceability back to spec.md
- Constitution Principle III makes the Domain/Application test tasks (T023, T024, T038, T045, T051) mandatory, not optional
- Each user story's Independent Test (stated in its phase header) is the acceptance bar before moving to the next priority
- Commit after each task or logical group
