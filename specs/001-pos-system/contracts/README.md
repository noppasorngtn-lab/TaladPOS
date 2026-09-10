# API Contracts: ระบบ POS (Single Store)

**Feature**: `001-pos-system`

All endpoints are exposed by `api/` (ASP.NET Core Web API) and are the **only** way `web/`
(Next.js) reads or writes data, per Constitution Principle I. Payloads are JSON. Except `POST
/auth/login`, every endpoint requires an `Authorization: Bearer <JWT>` header (research item 2).
Roles: `Cashier` (default) and `Admin` (superset of `Cashier`) per FR-034.

| File | Bounded context | Spec traceability |
|---|---|---|
| [auth.md](./auth.md) | Staff login | FR-007, FR-034 |
| [products.md](./products.md) | Product catalog & stock | FR-001–FR-014, FR-032 |
| [members.md](./members.md) | Membership | FR-015–FR-019 |
| [promotions.md](./promotions.md) | Promotions | FR-020–FR-024 |
| [sales-orders.md](./sales-orders.md) | Sales transactions & void | FR-002–FR-006, FR-025–FR-028, FR-036 |
| [reports.md](./reports.md) | Reporting | FR-029–FR-033 |

## Conventions

- Error responses use a consistent shape: `{ "error": { "code": string, "message": string } }`.
- `409 Conflict` is reserved for domain-rule violations that depend on concurrent state
  (insufficient stock, duplicate void), matching research item 4.
- `422 Unprocessable Entity` is reserved for request-level validation failures (duplicate
  barcode/phone, invalid discount percent, negative quantity).
- List endpoints support `page`/`pageSize` query parameters; date-range filters use
  `from`/`to` (ISO 8601 dates).
