# Implementation Plan: เมนูนำทางระบบและหน้าจอการจัดการสิทธิ์

**Branch**: `003-menu-permission-management` | **Date**: 2026-09-11 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-menu-permission-management/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Add a central, role-filtered navigation menu to `web/` (which today has none — every screen is an
independent route) so any logged-in staff member can reach every screen they have access to
without knowing URLs, and add a new Admin-only "จัดการสิทธิ์" screen backed by new Staff CRUD
endpoints on `api/` (create, edit name/role, activate/deactivate, reset password), reusing the
existing `Staff` entity and `Cashier`/`Admin` role enum rather than introducing a new permission
model. A new append-only `StaffAuditLog` records who changed what, with no viewing UI in this
version. Per the project constitution, all data access stays behind the existing DDD-layered
ASP.NET Core API (`api/`), and the frontend (`web/`, Next.js + Tailwind + PrimeReact) talks to it
only over REST/JSON.

## Technical Context

**Language/Version**: Backend: C# / .NET 10 (`net10.0`, all four `api/src/*.csproj`). Frontend:
TypeScript ^5 / Node.js 20 LTS.

**Primary Dependencies**: Backend: ASP.NET Core Web API, Entity Framework Core (Npgsql provider),
`Microsoft.AspNetCore.Authentication.JwtBearer`, xUnit (test-only) — all already in place, no new
package needed (password hashing reuses the existing `PasswordHasher`, PBKDF2, no new crypto
library). Frontend: Next.js 14.2.35 (App Router), React ^18, Tailwind CSS, PrimeReact ^10.9.9
(unstyled + `tailwindcss-primeui`) — the new `/staff` screen reuses the same `DataTable`/`Dialog`
components already used by `/stock`; no new dependency needed for the nav menu (PrimeReact
`Menubar`/plain `<nav>` with existing Tailwind classes).

**Storage**: PostgreSQL 16, accessed only from `api/` via EF Core code-first migrations (dev
connection: dedicated `taladpos-postgres` container, port 5433 — see quickstart.md). This feature
adds one new table (`StaffAuditLog`) and no schema change to `Staff` (same columns; new behavior
only).

**Testing**: xUnit unit tests for the Domain and Application layers are **mandatory** (Constitution
Principle III, NON-NEGOTIABLE) — new `Staff` mutation methods (`Rename`, `ChangeRole`, `Activate`,
`Deactivate`, `SetPasswordHash`) and the last-active-Admin guard in `ManageStaffUseCases` need unit
coverage in `TaladPOS.Domain.Tests` / `TaladPOS.Application.Tests`. API integration tests
(`TaladPOS.Api.IntegrationTests`, existing `SecurityTests.cs` pattern) should cover the
Admin-only `403` behavior for `/staff/*`. Frontend correctness is validated via
`quickstart.md`, optionally extended with a Playwright spec following the existing
`web/tests/security-rbac.spec.ts` pattern for the new Admin-only route guard.

**Target Platform**: Same as 001 — self-hosted web app for a single physical store; `web/` served
to POS terminals/tablets (cashiers) and a desktop (admin).

**Project Type**: Web application (frontend + backend), matching Constitution Principle V's
mandated `api/` + `web/` split — no new top-level project.

**Performance Goals**: SC-001 (menu → any screen in ≤2 clicks) is a UX/navigation-depth goal, not
a latency target; no new performance-sensitive path is introduced (`/staff` list is small-scale,
see Scale/Scope).

**Constraints**: All `web/`↔`api/` communication MUST be REST/JSON (Constitution Principle I) —
the menu's role filter reads `staff.role` already returned by `POST /auth/login`/`GET /auth/me`,
no new "who am I" endpoint. The last-active-Admin invariant (FR-012) MUST be enforced in the
Application layer, not the database, matching Constitution Principle III's "business logic has
unit tests, not DB triggers" pattern (research.md §5).

**Scale/Scope**: Single store; staff headcount in the tens at most (spec.md edge case anticipates
"หลายสิบ-หลายร้อยคน" as an upper bound to plan pagination for, not an expected steady-state); 6 new
API endpoints (`contracts/staff.md`), 1 new frontend route (`/staff`), 1 new shared component (nav
menu).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. Clear API/Frontend Separation | `web/` reaches Staff data only via `contracts/staff.md` endpoints; the nav menu's role filter uses `staff.role` already delivered by the existing `/auth` contract — no DB access added to `web/` | PASS |
| II. Domain-Driven Design for the API | New `Staff` mutation methods live on the Domain entity; `StaffAuditLog` is a new Domain entity; cross-aggregate rules (last-Admin guard, username uniqueness) live in the new `TaladPOS.Application/Staff/ManageStaffUseCases.cs`, not in Domain or Infrastructure (research.md §3–§5) | PASS |
| III. Test-First for Business Logic | New Domain mutation methods and the last-Admin guard are identified as requiring xUnit coverage before merge (Technical Context "Testing") | PASS |
| IV. Frontend Technology Standards | New `/staff` screen and nav menu are Next.js + Tailwind, reusing existing PrimeReact `DataTable`/`Dialog` in unstyled mode; no DB access, no new frontend framework | PASS |
| V. Repository & Folder Structure | All backend changes stay under `api/src/{TaladPOS.Domain,TaladPOS.Application,TaladPOS.Infrastructure,TaladPOS.Api}`; all frontend changes stay under `web/src/`; no cross-imports | PASS |

No violations — Complexity Tracking is not applicable (left empty below).

## Project Structure

### Documentation (this feature)

```text
specs/003-menu-permission-management/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   ├── README.md
│   └── staff.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
api/
├── src/
│   ├── TaladPOS.Domain/
│   │   └── Entities/
│   │       ├── Staff.cs              # EXTEND: add Rename/ChangeRole/Activate/Deactivate/SetPasswordHash
│   │       └── StaffAuditLog.cs      # NEW: append-only audit entity (data-model.md)
│   ├── TaladPOS.Application/
│   │   ├── Auth/
│   │   │   └── IStaffRepository.cs   # EXTEND: GetByIdAsync/SearchAsync/UsernameExistsAsync/
│   │   │                              #   CountActiveAdminsAsync/AddAsync/SaveChangesAsync
│   │   └── Staff/                    # NEW feature folder (mirrors Products/)
│   │       └── ManageStaffUseCases.cs   # Create/Edit/ChangeRole/Activate/Deactivate/ResetPassword
│   ├── TaladPOS.Infrastructure/
│   │   └── Persistence/
│   │       ├── Configurations/
│   │       │   └── StaffAuditLogConfiguration.cs   # NEW
│   │       ├── Migrations/                          # NEW migration: StaffAuditLog table
│   │       └── Repositories/
│   │           └── StaffRepository.cs               # EXTEND per IStaffRepository additions
│   └── TaladPOS.Api/
│       └── Controllers/
│           └── Staff/                # NEW (mirrors Controllers/Products/)
│               ├── StaffController.cs
│               └── StaffDtos.cs
└── tests/
    ├── TaladPOS.Domain.Tests/        # NEW: Staff mutation method tests
    ├── TaladPOS.Application.Tests/   # NEW: ManageStaffUseCases tests incl. last-Admin guard
    └── TaladPOS.Api.IntegrationTests/  # NEW: /staff Admin-only 403 tests (SecurityTests.cs pattern)

web/
├── src/
│   ├── app/(app)/
│   │   ├── layout.tsx           # EXTEND: render the new nav menu around {children}
│   │   └── staff/                # NEW route (mirrors stock/)
│   │       ├── layout.tsx        # Admin-only guard, copied from stock/layout.tsx
│   │       └── page.tsx          # Staff list + create/edit/deactivate/reset-password UI
│   ├── components/
│   │   ├── NavMenu.tsx           # NEW: renders navigation.ts items filtered by staff.role
│   │   └── StaffFormDialog.tsx   # NEW (mirrors ProductFormDialog.tsx)
│   └── lib/
│       ├── navigation.ts         # NEW: static menu item config (data-model.md "Menu Item")
│       └── api/
│           └── staff.ts          # NEW: typed client for contracts/staff.md
└── tests/
    └── security-rbac.spec.ts     # EXTEND (not new) — add /staff Cashier-redirect + 403 cases
```

**Structure Decision**: No new top-level directory — this feature extends the existing `api/`
(DDD-layered ASP.NET Core) and `web/` (Next.js + Tailwind) split from 001-pos-system, adding one
new Application feature folder (`Staff/`), one new Domain entity (`StaffAuditLog`), one new API
controller (`Controllers/Staff/`), and one new frontend route (`app/(app)/staff/`) plus a shared
`NavMenu` component consumed from the existing `(app)/layout.tsx`.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
