---

description: "Task list template for feature implementation"
---

# Tasks: เมนูนำทางระบบและหน้าจอการจัดการสิทธิ์

**Input**: Design documents from `/specs/003-menu-permission-management/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/staff.md, quickstart.md

**Tests**: Domain/Application unit tests ARE included below — Constitution Principle III
(NON-NEGOTIABLE) mandates automated unit test coverage for all Domain/Application business logic,
which this feature adds (Staff mutation methods, the last-active-Admin guard, username
uniqueness). API integration tests and a Playwright RBAC extension are also included, matching
the existing `SecurityTests.cs` / `security-rbac.spec.ts` conventions already used by this
codebase. Pure frontend UI (nav menu rendering, form dialogs) has no dedicated unit tests, matching
the existing convention (`web/` has no Jest/Vitest — see plan.md Technical Context).

**Organization**: Tasks are grouped by user story (US1 = P1 nav menu, US2 = P2 permission
management) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2)
- File paths are exact, relative to the repository root

---

## Phase 1: Setup

**Purpose**: Confirm the environment this feature builds on is ready. No new dependencies are
needed (research.md confirms the existing `.NET 10` / `Next.js 14.2.35` / PrimeReact stack covers
everything this feature requires) and no new top-level project is created.

- [X] T001 Confirm the dev environment is ready: `taladpos-postgres` PostgreSQL container reachable
      on port 5433, and `dotnet ef` tooling available for running the migration this feature adds
      (see quickstart.md Prerequisites). No code changes in this task.

---

## Phase 2: Foundational

**Purpose**: Cross-story blocking prerequisites.

**None required.** User Story 1 (nav menu) is frontend-only and User Story 2 (staff management)
is backend+frontend, but they share no *new* infrastructure — both read `staff.role` from the
`AuthProvider`/`GET /auth/me` that already exists from `001-pos-system`. Proceed directly to the
user story phases.

---

## Phase 3: User Story 1 - เข้าถึงทุกหน้าจอผ่านเมนูนำทางเดียว (Priority: P1) 🎯 MVP

**Goal**: A central, role-filtered navigation menu appears on every authenticated screen (never on
`/login`), highlights the current screen, and navigates client-side (FR-001–FR-005).

**Independent Test**: Log in as a `Cashier` and separately as an `Admin`; confirm each sees only
the menu items their role can access (per the *actual* existing per-screen guards — see T002), the
current screen is highlighted, and clicking an item navigates without a full page reload.

### Implementation for User Story 1

- [X] T002 [P] [US1] Create the static menu config in `web/src/lib/navigation.ts`: an array of
      `{ label, href, icon, allowedRoles }` covering all 6 screens, matching the **actual**
      role guard already enforced by each screen's `layout.tsx` (verified by reading the code, not
      assumed): `ขายสินค้า` → `/sales`, `allowedRoles: ["Cashier", "Admin"]` (no extra guard file —
      any authenticated staff); `สต็อกสินค้า` → `/stock`, `["Admin"]`; `โปรโมชั่น` → `/promotions`,
      `["Admin"]`; `ประวัติการขาย` → `/sales-history`, `["Admin"]`; `รายงาน` → `/reports`, `["Admin"]`;
      `จัดการสิทธิ์` → `/staff`, `["Admin"]` (new screen, built in User Story 2 below — the menu entry
      is added now so Admin's menu is complete per FR-002, even though `/staff` doesn't exist until
      US2 lands).
- [X] T003 [P] [US1] Create `web/src/components/NavMenu.tsx`: reads `useAuth().staff.role`, filters
      `navigation.ts` items to those whose `allowedRoles` includes the current role (FR-003), reads
      the current route via `usePathname()` to apply an active/highlighted style to the matching
      item (FR-004), and renders each item as a Next.js `<Link>` for client-side navigation
      (FR-005). Returns `null` while `isLoading` (mirrors the loading-guard style already used in
      `web/src/app/(app)/layout.tsx` and `stock/layout.tsx`).
- [X] T004 [US1] Render `<NavMenu />` from `web/src/app/(app)/layout.tsx`, wrapping `{children}`,
      so it appears on every screen inside the `(app)` route group and nowhere else — `/login` is
      outside this group and already unaffected (FR-001). (Depends on T002, T003.)

**Checkpoint**: User Story 1 is fully functional and independently testable — every existing
screen now has a working, role-filtered nav menu.

---

## Phase 4: User Story 2 - ผู้ดูแลระบบจัดการบัญชีพนักงานและสิทธิ์ (Priority: P2)

**Goal**: An Admin-only "จัดการสิทธิ์" screen backed by new Staff CRUD endpoints: list, create,
edit name/role, activate/deactivate (with a last-active-Admin guard), and reset password
(FR-006–FR-016).

**Independent Test**: As Admin, open `/staff` directly by URL, create a new Cashier account, log
in as that account, then deactivate it and confirm login is rejected; confirm a Cashier cannot
open `/staff` at all; confirm the last active Admin cannot deactivate or downgrade themself.

### Domain layer for User Story 2

- [X] T005 [P] [US2] Add mutation methods to `api/src/TaladPOS.Domain/Entities/Staff.cs`
      (currently only has a constructor and private setters — see data-model.md "Staff"):
      `Rename(string name)` (required, throws `ArgumentException` on blank/whitespace exactly like
      the constructor's existing `Name` check), `ChangeRole(StaffRole role)`, `Activate()`,
      `Deactivate()`, `SetPasswordHash(string passwordHash)` (required, throws `ArgumentException`
      on blank/whitespace exactly like the constructor's existing `PasswordHash` check) — mirror
      the style of `Product.Deactivate()` / `Promotion.Deactivate()` (single-field mutators; no
      cross-aggregate checks here, see T013).
- [X] T006 [P] [US2] Create `api/src/TaladPOS.Domain/Entities/StaffAuditLog.cs` per data-model.md
      "Staff Audit Log": `Id` (Guid, PK, system-generated), `StaffId` (Guid, the changed account),
      `PerformedByStaffId` (Guid, the acting Admin), `Action` (enum `StaffAuditAction` with exactly
      `Created`, `Edited`, `RoleChanged`, `Activated`, `Deactivated`, `PasswordReset`), `Timestamp`
      (UTC `DateTime`, set by the constructor to `DateTime.UtcNow`) — append-only, no setters
      beyond the constructor. Disambiguation for T013: when a single `EditAsync` call changes both
      `name` and `role`, log `RoleChanged` (role changes take precedence since they affect access,
      the more security-relevant fact); log `Edited` only when `role` is unchanged and `name`
      changed; log nothing if neither actually changed value.
- [X] T007 [US2] Unit tests in `api/tests/TaladPOS.Domain.Tests/StaffTests.cs` (new file, mirrors
      `ProductTests.cs`): `Rename` with blank/whitespace name throws `ArgumentException`; `Rename`
      with a valid name updates `Name`; `ChangeRole` updates `Role`; `Activate` sets `IsActive` to
      `true`; `Deactivate` sets `IsActive` to `false`; `SetPasswordHash` with blank hash throws
      `ArgumentException`, with a valid hash updates `PasswordHash`. Constitution Principle III
      (NON-NEGOTIABLE). (Depends on T005.)

### Repository layer for User Story 2

- [X] T008 [US2] Extend `IStaffRepository` in `api/src/TaladPOS.Application/Auth/IStaffRepository.cs`
      (currently only `FindByUsernameAsync`) with: `GetByIdAsync(Guid id, CancellationToken)`;
      `SearchAsync(string? search, int page, int pageSize, CancellationToken)` returning
      `(IReadOnlyList<Staff> Items, int Total)` (matches name or username, per contracts/staff.md
      `GET /staff`); `UsernameExistsAsync(string username, Guid? excludeStaffId, CancellationToken)`
      (backs the FR-009 uniqueness rule, `excludeStaffId` lets an edit keep its own username);
      `CountActiveAdminsAsync(CancellationToken)` (backs the FR-012 last-Admin guard);
      `AddAsync(Staff staff, CancellationToken)`; `SaveChangesAsync(CancellationToken)` — mirrors
      `IProductRepository`'s shape exactly (research.md §3).
- [X] T009 [US2] Implement the new `IStaffRepository` members in
      `api/src/TaladPOS.Infrastructure/Persistence/Repositories/StaffRepository.cs` against
      `TaladPOSDbContext.Staff`, using EF Core LINQ (`Where(name-contains OR username-contains)` for
      `SearchAsync`, `CountAsync(s => s.IsActive && s.Role == StaffRole.Admin)` for
      `CountActiveAdminsAsync`). (Depends on T008.)
- [X] T010 [P] [US2] Create
      `api/src/TaladPOS.Infrastructure/Persistence/Configurations/StaffAuditLogConfiguration.cs`:
      `Id` as primary key; `StaffId` and `PerformedByStaffId` as separate foreign keys to `Staff`,
      both with `DeleteBehavior.Restrict` (no cascade delete — data-model.md "no cascade delete",
      since Staff accounts are only ever soft-disabled, never hard-deleted); `Action` stored as a
      string column. (Depends on T006.)
- [X] T011 [US2] Add `public DbSet<StaffAuditLog> StaffAuditLogs => Set<StaffAuditLog>();` to
      `api/src/TaladPOS.Infrastructure/Persistence/TaladPOSDbContext.cs`. (Depends on T006.)
- [X] T012 [US2] Generate and review the EF Core migration for the new `StaffAuditLog` table:
      `dotnet ef migrations add AddStaffAuditLog --project src/TaladPOS.Infrastructure
      --startup-project src/TaladPOS.Api` (run from `api/`), then apply it locally
      (`dotnet ef database update ...`) against the `taladpos-postgres` dev database to confirm it
      succeeds. (Depends on T010, T011.)

### Application layer for User Story 2

- [X] T013 [US2] Create `api/src/TaladPOS.Application/StaffManagement/ManageStaffUseCases.cs`
      (renamed from the planned `Staff/` folder during implementation: `namespace
      TaladPOS.Application.Staff` collides with the `Staff` entity type in C# name resolution —
      every other file in `TaladPOS.Application` that says bare `Staff` unqualified, e.g.
      `IStaffRepository.cs`/`LoginUseCase.cs`, failed to compile once that namespace existed;
      `StaffManagement` avoids the collision), mirroring
      `Products/ManageProductUseCases.cs`'s structure (constructor-injected `IStaffRepository`,
      `record` request DTOs, `KeyNotFoundException` for a missing id): `CreateAsync` (checks
      `UsernameExistsAsync` first, FR-009), `EditAsync(id, name, role)` (checks
      `UsernameExistsAsync` is not applicable here since username isn't editable per
      contracts/staff.md `PUT /staff/{id}`; checks the FR-012 guard before applying a role change
      away from `Admin`), `DeactivateAsync(id)` (checks `CountActiveAdminsAsync` before
      `Deactivate()` when the target is an active Admin — FR-012), `ActivateAsync(id)`,
      `ResetPasswordAsync(id, newPasswordHash)` (FR-016). `CreateAsync` and `ResetPasswordAsync`
      also validate the incoming plaintext password is at least 8 characters (FR-015) before
      hashing — throw the same validation exception (422) used for duplicate username.
      `CreateAsync` additionally validates `username` has no whitespace and contains only
      letters/digits/`_`/`.`/`-` (FR-015, contracts/staff.md). Each successful mutation also constructs
      and persists a `StaffAuditLog` row (`Action` matching the operation, `PerformedByStaffId`
      passed in from the controller's JWT claims) — FR-014. Last-Admin violations throw a
      domain-conflict exception (mapped to `409` at the API layer); duplicate-username violations
      throw a validation exception (mapped to `422`). (Depends on T005, T006, T008, T009.)
- [X] T014 [US2] Unit tests in `api/tests/TaladPOS.Application.Tests/ManageStaffUseCasesTests.cs`
      (new file, using a fake in-memory `IStaffRepository`): create with a duplicate username
      throws the validation exception and does not call `AddAsync`; deactivating the sole active
      Admin throws the conflict exception and leaves `IsActive` unchanged; deactivating a
      non-last-Admin (or any Cashier) succeeds; changing the sole active Admin's role away from
      `Admin` throws the conflict exception; `ResetPasswordAsync` updates `PasswordHash`; every
      successful mutation adds exactly one `StaffAuditLog` entry with the correct `Action` (per the
      disambiguation rule in T006 — role-changing edits log `RoleChanged`, name-only edits log
      `Edited`); `CreateAsync`/`ResetPasswordAsync` with a password under 8 characters throws the
      validation exception; `CreateAsync` with a username containing whitespace or a character
      outside letters/digits/`_`/`.`/`-` throws the validation exception.
      Constitution Principle III (NON-NEGOTIABLE). (Depends on T013.)

### API layer for User Story 2

- [X] T015 [P] [US2] Create `api/src/TaladPOS.Api/Controllers/Staff/StaffDtos.cs` per
      contracts/staff.md: `StaffSummaryResponse(Guid id, string name, string username, string role,
      bool isActive)` (`role` serialized as its string name for the JSON response);
      `CreateStaffRequest(string name, string username, string password, StaffRole role)`;
      `EditStaffRequest(string name, StaffRole role)`; `ResetPasswordRequest(string newPassword)` —
      `role` is bound as the `StaffRole` enum (not `string`) so an invalid value fails ASP.NET Core
      model binding and returns `400` automatically (U3/contracts/staff.md), with no extra
      validation code needed.
- [X] T016 [US2] Create `api/src/TaladPOS.Api/Controllers/Staff/StaffController.cs`: `[Route("staff")]`,
      `[Authorize(Policy = "Admin")]` at the controller level (every endpoint in this file is
      Admin-only per contracts/staff.md — a `Cashier` JWT gets `403` on all of them, no per-action
      override like `ProductsController`'s search endpoint). Implements `GET /` (search + `page`/
      `pageSize`, 200), `GET /{id:guid}` (200/404), `POST /` (hashes the incoming password via the
      existing `IPasswordHasher` before calling `ManageStaffUseCases.CreateAsync`, 201/422),
      `PUT /{id:guid}` (200/409), `POST /{id:guid}/deactivate` (204/409), `POST /{id:guid}/activate`
      (204), `POST /{id:guid}/reset-password` (hashes via `IPasswordHasher`, 204/422) — mirrors
      `ProductsController.cs`'s structure. `role` is bound as `StaffRole` (not `string`); an
      unparseable value fails model binding and ASP.NET Core's default behavior returns `400`
      automatically (contracts/staff.md), so no extra code is needed for that case beyond typing
      the parameter correctly. Reads the acting Admin's id from `User` claims to pass as
      `PerformedByStaffId` into each use-case call. (Depends on T013, T015.)
- [X] T017 [US2] Register `ManageStaffUseCases` (and any newly-needed scoped services) in
      `api/src/TaladPOS.Api/Program.cs`, mirroring the existing `ManageProductUseCases`
      registration. (Depends on T013.)
- [X] T018 [P] [US2] Extend `api/tests/TaladPOS.Api.IntegrationTests/SecurityTests.cs` (same
      `ApiTestBase`/`CreateCashierClientAsync()` pattern already used for Products/Reports): add
      `Admin_only_staff_endpoints_reject_cashier_role_with_403` covering `GET /staff` and
      `POST /staff` with a Cashier JWT (SC-003). (Depends on T016.)
- [X] T019 [P] [US2] Create `api/tests/TaladPOS.Api.IntegrationTests/StaffTests.cs` (new file,
      mirrors `ProductsTests.cs`): `POST /staff` succeeds (201) and the new account can then log in
      via `POST /auth/login`; `POST /staff` with a duplicate username returns 422; `PUT /staff/{id}`
      changes the role and the account's next `GET /auth/me` reflects it; `POST
      /staff/{id}/deactivate` then a login attempt for that account returns 401 (already-existing
      `LoginUseCase.IsActive` check); `POST /staff/{id}/activate` restores login; deactivating (and
      separately, role-changing) the sole active Admin returns 409; `POST
      /staff/{id}/reset-password` followed by login with the new password succeeds and the old
      password no longer works; `POST /staff` with a 7-character password returns 422; `POST
      /staff` with a username containing a space returns 422; `POST /staff` with
      `role: "SuperAdmin"` returns 400. (Depends on T016, T012.)

### Frontend for User Story 2

- [X] T020 [P] [US2] Create `web/src/lib/api/staff.ts`: `StaffSummary` type
      (`{ id, name, username, role, isActive }`), `listStaff(token, search, page, pageSize)`,
      `getStaff(token, id)`, `createStaff(token, values)`, `updateStaff(token, id, values)`,
      `deactivateStaff(token, id)`, `activateStaff(token, id)`,
      `resetStaffPassword(token, id, newPassword)` — mirrors `web/src/lib/api/products.ts`,
      calling the `contracts/staff.md` endpoints through the existing `apiFetch` client.
- [X] T021 [P] [US2] Create `web/src/app/(app)/staff/layout.tsx`: Admin-only guard, copied in
      structure from `web/src/app/(app)/stock/layout.tsx` (`router.replace("/sales")` when
      `staff.role !== "Admin"`, renders `null` while loading or non-Admin) — FR-006.
- [X] T022 [P] [US2] Create `web/src/components/StaffFormDialog.tsx` (mirrors
      `ProductFormDialog.tsx`): on create, fields for name, username, password, and a role dropdown
      (`Cashier`/`Admin`); on edit, the same name/role fields but no password field (password reset
      is a separate action, not part of this dialog, per clarification Q1). On save, catch a `422`
      from `createStaff`/`updateStaff` (duplicate username, invalid username format, password under
      8 characters — FR-009, FR-015) and render the API's error message inline in the dialog rather
      than closing it, mirroring how `StockPage`'s `error` state surfaces `ApiError` messages.
- [X] T023 [US2] Create `web/src/app/(app)/staff/page.tsx` (mirrors `stock/page.tsx`): `DataTable`
      of staff (name, username, role, active/inactive status), a search box, an "Add staff" button
      opening `StaffFormDialog`, per-row Edit/Deactivate/Activate/"Reset password" actions wired to
      `lib/api/staff.ts`. Surfaces the API's `409` error message as-is when the last-Admin guard is
      hit, rather than re-implementing that check client-side. (Depends on T020, T021, T022.)

**Checkpoint**: User Stories 1 AND 2 both work independently — the nav menu (US1) now has a live
`จัดการสิทธิ์` destination, and `/staff` is fully usable by direct URL even without the menu.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Verification that spans both user stories.

- [X] T024 [P] Extend `web/tests/security-rbac.spec.ts`: in the "Security / RBAC — UI (Cashier
      session)" describe block, add `"/staff redirects to /sales"` (mirrors the existing `/stock`,
      `/promotions`, etc. tests); in the "Security / RBAC — API" describe block, add `/staff` and
      `POST /staff` to the Cashier-403 assertions; add a new assertion (in the same file or a new
      describe block) that an Admin session's rendered nav menu contains all 6 items including
      `จัดการสิทธิ์`, and a Cashier session's nav menu contains only `ขายสินค้า`. (Depends on T004,
      T021, T023 all being complete.)
- [X] T025 Run `dotnet test` in `api/` and `npm run lint` in `web/`; fix any failures before
      considering this feature done (Constitution "Development Workflow & Quality Gates").
- [X] T026 Run the full `quickstart.md` validation walkthrough (steps 1–7) end-to-end against a
      real running `api/` + `web/` + `taladpos-postgres`; fix any discrepancy found between the
      walkthrough and actual behavior.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Empty — nothing blocks the user stories beyond Setup.
- **User Story 1 (Phase 3)**: Depends on Setup only. Fully independent of User Story 2's backend
  work (T002's `navigation.ts` references `/staff` as a route string, not a code dependency).
- **User Story 2 (Phase 4)**: Depends on Setup only. Independently testable by direct URL
  (`/staff`) even before User Story 1's `NavMenu` exists.
- **Polish (Phase 5)**: Depends on both User Story 1 and User Story 2 being complete (T024 in
  particular exercises both).

### Within User Story 2 (the only story with internal layering)

Domain (T005–T007) → Repository (T008–T012) → Application (T013–T014) → API (T015–T019) →
Frontend (T020–T023), with the parallel-marked tasks in each layer safe to run together (different
files, no shared state).

### Parallel Opportunities

- T002 and T003 (User Story 1) — different files.
- T005 and T006 (User Story 2 Domain) — different files.
- T010 (EF configuration) can run alongside T008/T009 (repository) since it only depends on T006.
- T014, T015, T018, T019 mark independent files/concerns once their respective dependencies land.
- T020, T021, T022 (User Story 2 frontend) — different files, no shared state until T023 wires
  them together.
- User Story 1 (Phase 3) and User Story 2 (Phase 4) can be built by two people in parallel — they
  touch disjoint files until T024 in Polish.

---

## Parallel Example: User Story 2 Domain Layer

```bash
Task: "Add mutation methods to api/src/TaladPOS.Domain/Entities/Staff.cs"
Task: "Create api/src/TaladPOS.Domain/Entities/StaffAuditLog.cs"
```

## Parallel Example: User Story 2 Frontend

```bash
Task: "Create web/src/lib/api/staff.ts"
Task: "Create web/src/app/(app)/staff/layout.tsx"
Task: "Create web/src/components/StaffFormDialog.tsx"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1: Setup.
2. Complete Phase 3: User Story 1 (nav menu).
3. **STOP and VALIDATE**: every existing screen now has a working, role-filtered menu. Note that
   the `จัดการสิทธิ์` menu entry will 404 until User Story 2 ships — acceptable for an MVP checkpoint
   since spec.md prioritizes the menu itself (P1) over the screen it links to (P2), but flag this
   explicitly if demoing to a stakeholder before US2 lands.
4. Deploy/demo if ready.

### Incremental Delivery

1. Setup → User Story 1 → validate/demo (menu works, `จัดการสิทธิ์` link present but 404s).
2. Add User Story 2 → validate/demo (`/staff` fully functional; `จัดการสิทธิ์` link now works end to
   end).
3. Polish (T024–T026) → final regression + quickstart validation.

### Parallel Team Strategy

With two developers: one takes Phase 3 (User Story 1, frontend-only, small), the other takes Phase
4 (User Story 2, the larger backend+frontend slice) — both start right after Phase 1, and only
converge at Phase 5 (T024).
