# Contract: Products & Stock

Traceability: FR-001–FR-014, FR-032

## GET /products

List/search products for the sales screen and stock management screen.

- **Auth**: any authenticated staff
- **Query params**: `search` (matches name or barcode, FR-002), `includeInactive` (Admin only,
  default false), `page`, `pageSize`
- **Response 200**: `{ "items": [ProductSummary], "total": int }`
  - `ProductSummary`: `{ id, name, imageUrl, price, quantityOnHand, barcode, lowStock: bool }`

## GET /products/{id}

- **Auth**: any authenticated staff
- **Response 200**: full `Product` (adds `lowStockThreshold`, `isActive`)
- **Response 404**: not found

## POST /products

Add a new product (FR-009).

- **Auth**: Admin
- **Request body**: `{ name, price, quantityOnHand, barcode?, lowStockThreshold?, image (multipart) }`
- **Response 201**: created `Product`
- **Response 422**: validation failure (e.g., duplicate barcode — FR-014, price <= 0)

## PUT /products/{id}

Edit an existing product (FR-010).

- **Auth**: Admin
- **Request body**: same shape as POST, all fields optional except what changes
- **Response 200**: updated `Product`
- **Response 422**: duplicate barcode with another product

## DELETE /products/{id}

Soft-delete a product — removes it from sale without touching historical `SalesOrderLine` rows
(FR-011).

- **Auth**: Admin
- **Response 204**: no content
- **Response 409**: (not expected — delete is always a soft delete, never blocked by history)

## GET /products/low-stock

List products at or below their `lowStockThreshold`, for the Stock Management dashboard
indicator (FR-013, research item 7).

- **Auth**: Admin
- **Response 200**: `{ "items": [ProductSummary] }`
