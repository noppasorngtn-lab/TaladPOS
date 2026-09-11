# Phase 0 Research: เมนูนำทางระบบและหน้าจอการจัดการสิทธิ์

**Feature**: `003-menu-permission-management` | **Spec**: [spec.md](./spec.md)

This feature has no unresolved `NEEDS CLARIFICATION` items in Technical Context — the stack,
versions, and conventions are already fixed by the existing codebase (see plan.md Technical
Context) and by the constitution. Research below documents the concrete integration decisions
made by reading the current `api/` and `web/` code, so Phase 1 design and `/speckit-tasks` can
build directly on real files instead of re-deriving conventions.

## 1. Central navigation menu — where it lives in `web/`

**Decision**: Add the nav menu as a new component rendered from the existing shared route group
layout `web/src/app/(app)/layout.tsx`, not as a new top-level layout.

**Rationale**: `(app)/layout.tsx` already wraps every authenticated screen (sales, stock,
promotions, sales-history, reports) and already knows `token`/`isLoading`/redirects to `/login`
when unauthenticated (FR-001, FR-... "no menu on the login page" is satisfied automatically since
this layout never renders on `/login`). Adding the menu here means zero per-screen changes to
existing pages, and the new `staff` screen inherits it the same way.

**Alternatives considered**: A new root layout wrapping `(app)` — rejected, would duplicate the
auth-check logic that already exists; a menu embedded per-page — rejected, violates DRY and each
existing screen would need editing.

## 2. Role-based menu filtering — data source

**Decision**: Menu items are a static, hard-coded TypeScript array (icon, label, href, allowed
roles) in a new `web/src/lib/navigation.ts`, filtered client-side against `useAuth().staff.role`
(already exposed by `AuthProvider` — see `web/src/lib/auth/AuthProvider.tsx`).

**Rationale**: Spec Assumptions state the menu is not dynamically customizable in this version
(fixed set of existing screens); `staff.role` is already returned by `POST /auth/login` and
`GET /auth/me` (`specs/001-pos-system/contracts/auth.md`), so no new endpoint or entity is needed
to know "what can I see." This keeps the "Menu Item" key entity in spec.md as a config concern,
not a persisted domain entity.

**Alternatives considered**: Server-driven menu (API endpoint returning visible items) — rejected
as over-engineering for a fixed, small screen set; revisit only if the app later needs
admin-configurable menus.

## 3. Staff CRUD — repository shape

**Decision**: Extend the existing `IStaffRepository` (`api/src/TaladPOS.Application/Auth/IStaffRepository.cs`,
currently only `FindByUsernameAsync`) with `GetByIdAsync`, `SearchAsync` (paged, optional name/username
filter), `UsernameExistsAsync(username, excludeStaffId)`, `CountActiveAdminsAsync`, `AddAsync`,
`SaveChangesAsync` — mirroring the exact shape of `IProductRepository`
(`api/src/TaladPOS.Application/Products/IProductRepository.cs`).

**Rationale**: `Staff` is a single aggregate already used by `Auth/LoginUseCase`; introducing a
second, parallel repository interface over the same table (e.g. `IStaffAdminRepository`) would
duplicate the EF Core mapping surface for no benefit. Extending the existing interface keeps one
repository per aggregate, consistent with `IProductRepository`/`IPromotionRepository`.

**Alternatives considered**: New `Staff/` feature folder with its own repository interface —
rejected for the reason above; the use-case orchestration itself still gets its own
`TaladPOS.Application/Staff/` folder (see §4), only the repository interface stays in `Auth/`.

## 4. Staff CRUD — use case / domain shape

**Decision**: New `TaladPOS.Application/Staff/ManageStaffUseCases.cs` (Create/Edit/ChangeRole
would be folded into Edit/ResetPassword/SetActive methods), mirroring
`Products/ManageProductUseCases.cs` 1:1 in structure (constructor-injected repository, `record`
request DTOs, `KeyNotFoundException` for missing id). New `Staff` domain methods
(`api/src/TaladPOS.Domain/Entities/Staff.cs`, currently has only a constructor and private
setters) mirroring `Product.Deactivate()` / `Promotion.Deactivate()`:
`Rename(string name)`, `ChangeRole(StaffRole role)`, `Activate()`, `Deactivate()`,
`SetPasswordHash(string passwordHash)`.

**Rationale**: Matches the established Domain/Application layering (Constitution Principle II) —
mutation methods on the entity enforce field-level invariants (non-empty name), while
cross-aggregate invariants (see §5) stay in the use case, which can query the repository.

## 5. "At least one active Admin" invariant — where it's enforced

**Decision**: Enforced in `ManageStaffUseCases` (Application layer), not on the `Staff` entity
itself, by calling `IStaffRepository.CountActiveAdminsAsync()` before `Deactivate()` or
`ChangeRole()` away from `Admin`, and throwing a domain-rule exception mapped to `409 Conflict`
(same convention as stock-insufficiency in `specs/001-pos-system/contracts/README.md`) when the
target staff member is the last active Admin.

**Rationale**: This invariant spans multiple `Staff` rows (a count across the aggregate root's
table), which a single entity instance cannot see — Domain entities in this codebase never take a
repository dependency (Constitution Principle II: Domain must not depend on persistence). The
Application layer is the correct place for a check that requires querying other instances of the
same aggregate.

**Alternatives considered**: A DB-level constraint (e.g., trigger) — rejected as inconsistent with
the existing "no DB-only business rules" pattern (all rules elsewhere are enforced in
Domain/Application C# code with unit tests, Constitution Principle III).

## 6. Password reset — auth impact

**Decision**: `ManageStaffUseCases.ResetPasswordAsync(id, newPassword)` reuses the existing
`IPasswordHasher` (`api/src/TaladPOS.Infrastructure/Auth/PasswordHasher.cs`, PBKDF2) to hash the
Admin-supplied new password and calls `staff.SetPasswordHash(...)`. No change to
`LoginUseCase` or the JWT flow is required — login already verifies via `passwordHasher.Verify`
against whatever hash is currently stored.

**Rationale**: Confirmed by reading `api/src/TaladPOS.Application/Auth/LoginUseCase.cs` — it
already checks `!staff.IsActive` and rejects login for disabled accounts (FR-011's login-rejection
behavior is **already implemented**, not new work for this feature). Password reset only needs to
replace the stored hash; the rest of the login path is unchanged.

**Alternatives considered**: Forced-reset-on-next-login (temporary password + must-change flow) —
rejected per clarification Q1 (spec.md), deferred to a future version.

## 7. Staff Audit Log — persistence shape

**Decision**: New Domain entity `StaffAuditLog` (Id, StaffId, PerformedByStaffId, Action enum
`{Created, Edited, RoleChanged, Activated, Deactivated, PasswordReset}`, Timestamp), written by
`ManageStaffUseCases` after each successful mutation, persisted via a new EF Core configuration +
migration. No API endpoint exposes it in this version (per clarification Q2).

**Rationale**: Spec.md lists this as a Key Entity with explicit fields (who/what/when); modeling it
as a real entity (rather than e.g. unstructured logging) keeps it queryable later without a schema
migration when a future version adds a viewing UI, while costing only one small table now.

**Alternatives considered**: Structured application logging (e.g., Serilog to file) instead of a
DB table — rejected because spec.md's Key Entity explicitly frames this as data ("บันทึกว่าใครเป็น
ผู้กระทำ... เมื่อใด"), and the project already uses PostgreSQL as the system of record for all other
entities (Constitution Principle II); a DB table keeps a single source of truth.

## 8. API contract shape — action endpoints vs. generic PATCH

**Decision**: `POST /staff/{id}/deactivate`, `POST /staff/{id}/activate`,
`POST /staff/{id}/reset-password` as explicit action endpoints, alongside
`POST /staff` (create) and `PUT /staff/{id}` (edit name/role) — see `contracts/staff.md`.

**Rationale**: `IsActive` needs to toggle **both directions** (FR-011), unlike `Product`'s one-way
`DELETE` (soft-delete only, never reactivated) — a single `DELETE` verb doesn't fit. Explicit
action endpoints keep each operation's authorization/validation/audit-log side effect obvious and
independently testable, consistent with how `sales-orders.md` (001) likely models void as an
action rather than a generic PATCH (same style already used for state-changing operations in this
codebase).

**Alternatives considered**: `PATCH /staff/{id}` with a partial body (`{ isActive?, role? }`) —
rejected, mixes multiple distinct authorization/audit concerns behind one generic endpoint and
makes the 409 last-admin-guard harder to scope to the specific action that triggered it.

## 9. Frontend admin-only guard for the new `/staff` screen

**Decision**: `web/src/app/(app)/staff/layout.tsx`, copied verbatim in structure from
`web/src/app/(app)/stock/layout.tsx` (redirect to `/sales` if `staff.role !== "Admin"`).

**Rationale**: Identical requirement (FR-006: Admin-only, both via menu and direct URL) to the
existing Stock/Promotions guard pattern — no new pattern needed.
