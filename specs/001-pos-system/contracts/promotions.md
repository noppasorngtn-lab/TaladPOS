# Contract: Promotions

Traceability: FR-020–FR-024

## GET /promotions

List promotions (for management screen); includes past/future/active by default.

- **Auth**: Admin
- **Query params**: `activeOnly` (bool, filters to `startDate <= today <= endDate`)
- **Response 200**: `{ "items": [Promotion] }`

## POST /promotions

Create a promotion (FR-020, FR-021, FR-022).

- **Auth**: Admin
- **Request body**: `{ scope: "PerProduct"|"WholeBill"|"MemberDiscount", productId?: string,
  discountPercent: number, startDate: date, endDate: date }`
- **Response 201**: created `Promotion`
- **Response 422**: `endDate < startDate`; `productId` missing when `scope = PerProduct`;
  `productId` present when `scope != PerProduct`; `discountPercent` outside `(0, 100]`

## PUT /promotions/{id}

Edit a promotion's discount percent or date range.

- **Auth**: Admin
- **Response 200**: updated `Promotion`
- **Response 422**: same validation as POST

## DELETE /promotions/{id}

Deactivate a promotion (`isActive = false`) without deleting the record, so past sales that
referenced it remain explainable.

- **Auth**: Admin
- **Response 204**: no content

## GET /promotions/applicable?date={date}

Used internally by the sales-order pricing flow (research item 5) to fetch promotions whose
window covers `date` — exposed for the frontend to preview applicable discounts before checkout.

- **Auth**: any authenticated staff
- **Response 200**: `{ "items": [Promotion] }`
