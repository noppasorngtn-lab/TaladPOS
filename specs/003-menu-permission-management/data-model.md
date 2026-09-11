# Data Model: เมนูนำทางระบบและหน้าจอการจัดการสิทธิ์

**Feature**: `003-menu-permission-management` | **Spec**: [spec.md](./spec.md)

## Staff *(existing entity — extended by this feature)*

Already defined in `specs/001-pos-system/data-model.md` and `api/src/TaladPOS.Domain/Entities/Staff.cs`.
This feature adds no new fields — it adds the management (CRUD + role change + activate/deactivate
+ password reset) operations over the existing shape, plus the mutation methods needed to support
them (research.md §4).

| Field | Type | Rules |
|---|---|---|
| Id | Guid (PK) | System-generated |
| Name | string | Required (display name) |
| Username | string | Required; unique across all Staff (FR-009) |
| PasswordHash | string | Required; never exposed via API; set on create and on reset (FR-016) |
| Role | enum (`Cashier`, `Admin`) | Required; changeable by Admin (FR-010), except the last active Admin cannot be changed away from `Admin` (FR-012) |
| IsActive | bool | Toggle both directions (FR-011); disabled accounts are rejected at login (already enforced by `LoginUseCase`) |

**New domain methods** (on `Staff`, mirroring `Product`/`Promotion` mutation style):

- `Rename(string name)` — FR-010
- `ChangeRole(StaffRole role)` — FR-010, FR-013 (role stays within existing `Cashier`/`Admin` enum)
- `Activate()` / `Deactivate()` — FR-011
- `SetPasswordHash(string passwordHash)` — FR-008 (create), FR-016 (reset)

**Cross-aggregate validation rules** (enforced in `ManageStaffUseCases`, not on the entity —
research.md §5):

- `Username` unique on create and edit (FR-009) → `409 Conflict` in the API layer style already
  used for domain-rule violations (see contracts/staff.md).
- At least one active `Admin` must always exist (FR-012) → blocks `Deactivate()` and
  `ChangeRole()` away from `Admin` when the target is the last active Admin.

## Staff Audit Log *(new entity)*

Records who performed a create/edit/role-change/activate/deactivate/password-reset action on a
Staff account, and when (FR-014). Stored only — no API/UI surfaces it in this version
(clarification Q2, spec.md).

| Field | Type | Rules |
|---|---|---|
| Id | Guid (PK) | System-generated |
| StaffId | Guid (FK → Staff) | The account that was changed |
| PerformedByStaffId | Guid (FK → Staff) | The Admin who made the change |
| Action | enum (`Created`, `Edited`, `RoleChanged`, `Activated`, `Deactivated`, `PasswordReset`) | What happened |
| Timestamp | datetime (UTC) | System-generated, set at write time |

**Validation rules**: append-only; no update/delete operations in this version.

**Relationships**: many `StaffAuditLog` rows per `Staff` (both as the changed account and,
separately, as the acting Admin) — two FKs to the same `Staff` table, no cascade delete (Staff
accounts are soft-disabled, never hard-deleted, per spec.md Assumptions).

## Menu Item *(frontend configuration — not a persisted entity)*

Per spec.md Assumptions (menu is a fixed set of existing screens, not admin-customizable in this
version) and research.md §2, this is a static TypeScript config, not a database table:

| Field | Type | Notes |
|---|---|---|
| label | string | Display text (Thai) |
| href | string | Next.js route, e.g. `/sales`, `/staff` |
| allowedRoles | `StaffRole[]` | `["Cashier", "Admin"]` or `["Admin"]` |
| icon | string | PrimeIcons class name, matching existing UI's icon usage |

No migration or API contract needed for this "entity" — it lives entirely in
`web/src/lib/navigation.ts` and is filtered against `useAuth().staff.role` at render time.
