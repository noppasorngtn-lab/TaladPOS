# Contract: Reports

Traceability: FR-029–FR-033. All reports exclude `Voided` sales orders (FR-033).

## GET /reports/sales-summary

Daily/monthly total sales (FR-029).

- **Auth**: Admin
- **Query params**: `granularity` (`daily`|`monthly`), `from`, `to`
- **Response 200**: `{ "items": [ { "period": "2026-09-10", "totalSales": number, "orderCount": int } ] }`

## GET /reports/best-sellers

Products ranked by quantity/amount sold (FR-030).

- **Auth**: Admin
- **Query params**: `from`, `to`, `limit` (default 20)
- **Response 200**: `{ "items": [ { "productId", "productName", "quantitySold", "totalAmount" } ] }`

## GET /reports/sales-by-staff

Sales totals grouped by staff member (FR-031).

- **Auth**: Admin
- **Query params**: `from`, `to`
- **Response 200**: `{ "items": [ { "staffId", "staffName", "orderCount", "totalSales" } ] }`

## GET /reports/stock-levels

Current on-hand quantity for every active product (FR-032).

- **Auth**: Admin
- **Response 200**: `{ "items": [ { "productId", "productName", "quantityOnHand", "lowStock": bool } ] }`
