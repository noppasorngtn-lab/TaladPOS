# Contract: Staff Management

Traceability: FR-006–FR-016

All endpoints below require `Authorization: Bearer <JWT>` with the `Admin` role. A `Cashier` token
receives `403 Forbidden` on every endpoint in this file (FR-006, SC-003).

## GET /staff

List/search staff accounts for the permission management screen (FR-007).

- **Auth**: Admin
- **Query params**: `search` (matches name or username), `page`, `pageSize`
- **Response 200**: `{ "items": [StaffSummary], "total": int }`
  - `StaffSummary`: `{ id, name, username, role, isActive }`

## GET /staff/{id}

- **Auth**: Admin
- **Response 200**: `StaffSummary` (see above — no field beyond the list view is needed since
  `PasswordHash` is never exposed)
- **Response 404**: not found

## POST /staff

Add a new staff account (FR-008).

- **Auth**: Admin
- **Request body**: `{ name, username, password, role }` (`role`: `"Cashier"` | `"Admin"`;
  `username`: no whitespace, only letters/digits/`_`/`.`/`-` (FR-015); `password`: at least 8
  characters, no other complexity rule (FR-015))
- **Response 201**: created `StaffSummary`
- **Response 400**: `role` is not `"Cashier"` or `"Admin"` (malformed request — not a domain rule,
  so `400` not `422`, consistent with standard model-binding failures)
- **Response 422**: duplicate username (FR-009), username fails the format rule above, or password
  shorter than 8 characters

## PUT /staff/{id}

Edit an existing staff account's display name and/or role (FR-010).

- **Auth**: Admin
- **Request body**: `{ name, role }`
- **Response 200**: updated `StaffSummary`
- **Response 400**: `role` is not `"Cashier"` or `"Admin"`
- **Response 409**: request would change role away from `Admin` on the last active Admin (FR-012)

## POST /staff/{id}/deactivate

Disable a staff account so it can no longer log in (FR-011).

- **Auth**: Admin
- **Response 204**: no content
- **Response 409**: target is the last active Admin (FR-012)

## POST /staff/{id}/activate

Re-enable a previously disabled staff account.

- **Auth**: Admin
- **Response 204**: no content

## POST /staff/{id}/reset-password

Set a new password for an existing staff account (FR-016, clarification Q1 — Admin sets it
directly, staff can log in with it immediately, no forced-change-on-next-login flow).

- **Auth**: Admin
- **Request body**: `{ newPassword }`
- **Response 204**: no content
- **Response 422**: `newPassword` shorter than 8 characters (same rule as `POST /staff`)
