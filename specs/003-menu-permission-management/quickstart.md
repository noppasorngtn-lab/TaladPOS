# Quickstart: เมนูนำทางระบบและหน้าจอการจัดการสิทธิ์

**Feature**: `003-menu-permission-management` | **Purpose**: End-to-end validation that the
feature works, walking through each user story from `spec.md` against the contracts in
`contracts/` and the data model in `data-model.md`.

## Prerequisites

- PostgreSQL (dedicated `taladpos-postgres` container, port 5433) running, EF Core migrations for
  this feature applied (`dotnet ef database update --project src/TaladPOS.Infrastructure
  --startup-project src/TaladPOS.Api`, run from `api/`).
- `api/` solution built (`dotnet run --project src/TaladPOS.Api`).
- `web/` dependencies installed (`npm install`) and running (`npm run dev`).
- A seeded `Admin` staff account (`AdminSeed.cs`) to bootstrap the first login.

## Run

```bash
# Terminal 1 — API
cd api
dotnet run --project src/TaladPOS.Api

# Terminal 2 — Frontend
cd web
npm run dev
```

Open `http://localhost:3000`.

## Validation Walkthrough

### 1. Menu visibility by role (User Story 1, contracts/auth.md)

1. Log in as the seeded Admin. Confirm the nav menu shows all screens: ขายสินค้า, สต็อกสินค้า,
   โปรโมชั่น, ประวัติการขาย, รายงาน, จัดการสิทธิ์ (spec.md US1 scenario 2).
2. Confirm the current screen's menu item is visually highlighted (US1 scenario 3), and clicking
   another item navigates without a full page reload (US1 scenario 4).
3. Log out, log in as a `Cashier`. Confirm the nav menu shows only ขายสินค้า — สต็อกสินค้า,
   โปรโมชั่น, ประวัติการขาย, รายงาน, and จัดการสิทธิ์ are all absent (US1 scenario 1).
4. Log out. Confirm no nav menu renders on `/login` and any direct navigation to an authenticated
   route redirects to `/login` (US1 scenario 5 — pre-existing `(app)/layout.tsx` behavior).

### 2. Staff list (User Story 2, contracts/staff.md)

1. As Admin, open จัดการสิทธิ์ (`/staff`). Confirm `GET /staff` returns the seeded Admin and any
   staff created in later steps, each with username, role, and active status (US2 scenario 1).
2. As the `Cashier` created in step 1.3, attempt to open `/staff` directly by URL. Confirm access
   is denied and no staff data is shown (US2 scenario 7, SC-003).

### 3. Create staff account (User Story 2, contracts/staff.md)

1. As Admin, create a new account with a unique username and `role: Cashier`
   (`POST /staff`). Confirm `201` and the account appears in the list (US2 scenario 2).
2. Log out, log in as the new Cashier account; confirm login succeeds and the menu matches the
   Cashier role (SC-002).
3. As Admin, attempt to create another account reusing the same username. Confirm `422` and no
   duplicate account is created (US2 scenario 3, FR-009).

### 4. Change role (User Story 2, contracts/staff.md)

1. As Admin, edit the Cashier account from step 3 to `role: Admin` (`PUT /staff/{id}`). Confirm
   `200`.
2. Have that staff member log in again (or re-fetch `GET /auth/me`); confirm their menu now
   includes the Admin-only items (US2 scenario 4).

### 5. Deactivate / reactivate + login rejection (User Story 2, contracts/staff.md)

1. As Admin, deactivate a non-Admin staff account (`POST /staff/{id}/deactivate`). Confirm `204`.
2. Attempt to log in as that account; confirm the login is rejected (US2 scenario 5 — already
   enforced by the existing `LoginUseCase.IsActive` check, research.md §6).
3. Reactivate the account (`POST /staff/{id}/activate`); confirm login now succeeds again.

### 6. Last-Admin protection (User Story 2 edge cases, contracts/staff.md)

1. Ensure only one active Admin exists (deactivate/downgrade any others via steps above, or use a
   fresh seed).
2. As that Admin, attempt to deactivate your own account (`POST /staff/{id}/deactivate`). Confirm
   `409` (US2 scenario 6, FR-012).
3. Attempt to change your own role to `Cashier` (`PUT /staff/{id}`). Confirm `409` (spec.md edge
   case).

### 7. Password reset (User Story 2 scenario 8, contracts/staff.md)

1. As Admin, reset another staff member's password (`POST /staff/{id}/reset-password`). Confirm
   `204`.
2. Log in as that staff member with the new password; confirm success and that the old password
   no longer works.

## Success Criteria Mapping

| Quickstart step | Success Criteria |
|---|---|
| 1 | SC-001, SC-003 |
| 3 | SC-002 |
| 6 | SC-004 |
| 5 | SC-005 |
