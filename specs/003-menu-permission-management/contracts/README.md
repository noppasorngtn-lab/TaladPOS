# API Contracts: เมนูนำทางระบบและหน้าจอการจัดการสิทธิ์

**Feature**: `003-menu-permission-management`

New endpoints are exposed by `api/` (ASP.NET Core Web API) and are the only way `web/` reads or
writes Staff account data, per Constitution Principle I. Conventions below extend (do not
replace) `specs/001-pos-system/contracts/README.md`.

| File | Bounded context | Spec traceability |
|---|---|---|
| [staff.md](./staff.md) | Staff account management (permission management screen) | FR-006–FR-016 |

`GET /auth/me` and `POST /auth/login` (`specs/001-pos-system/contracts/auth.md`) are unchanged and
reused as-is: the frontend's role-based menu filtering (FR-003) and the `/staff` screen's
Admin-only guard (FR-006) both read `staff.role` from those existing responses — no new
"who am I" endpoint is introduced.

## Conventions (in addition to 001's)

- All `/staff` endpoints require `Authorization: Bearer <JWT>` with the `Admin` role
  (`[Authorize(Policy = "Admin")]`, same policy already registered in `Program.cs`).
- `422 Unprocessable Entity` additionally covers: duplicate username on create/edit (FR-009),
  matching 001's "duplicate barcode" precedent.
- `409 Conflict` additionally covers: any attempt to deactivate or change the role of the last
  active Admin (FR-012) — a concurrent-state domain rule (the count of other active Admins can
  change between page load and submit), matching 001's "insufficient stock" precedent.
- List endpoint (`GET /staff`) supports `search`, `page`, `pageSize` query parameters, matching
  `GET /products`' shape.
