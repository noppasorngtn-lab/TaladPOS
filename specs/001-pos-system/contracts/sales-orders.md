# Contract: Sales Orders (Checkout, History, Void)

Traceability: FR-002–FR-006, FR-025–FR-028, FR-036

## POST /sales-orders

Create (checkout) a sales order. This is the single write path that decrements stock, applies
promotions/member discount, and credits the member's accumulated total — all in one transaction
(research items 4–5).

- **Auth**: any authenticated staff (the authenticated staff member becomes `StaffId`, FR-008)
- **Request body**:
  ```json
  {
    "memberId": "string | null",
    "lines": [ { "productId": "string", "quantity": "int > 0" } ]
  }
  ```
- **Response 201**: full `SalesOrder` including computed `subtotalAmount`,
  `promotionDiscountAmount`, `memberDiscountAmount`, `netTotal`
- **Response 409**: any line's `quantity` exceeds current `QuantityOnHand` (FR-005) — response
  identifies which product(s) failed so the client can adjust the cart
- **Response 422**: empty `lines`, non-positive `quantity`, unknown `productId`/`memberId`
- **Response 503**: server unreachable is a client-observed condition, not a server response —
  the frontend MUST treat any failed request (network error) as "reject the whole transaction,
  do not retry partially" per FR-006/SC-007

## GET /sales-orders

Search sale history (FR-026).

- **Auth**: Admin
- **Query params**: `from`, `to` (dates), `staffId?`, `memberId?`, `status?` (`Completed`|`Voided`), `page`, `pageSize`
- **Response 200**: `{ "items": [SalesOrderSummary], "total": int }`

## GET /sales-orders/{id}

Full detail of one bill, including lines, discounts, staff, member (FR-025).

- **Auth**: Admin, or the `Cashier` who created it
- **Response 200**: full `SalesOrder` + `lines: [SalesOrderLine]`
- **Response 404**: not found

## POST /sales-orders/{id}/void

Void a completed order in full (FR-027, FR-028).

- **Auth**: Admin
- **Response 200**: updated `SalesOrder` with `status: "Voided"`, `voidedAt` set; stock for every
  line restored; if a member was linked, their `accumulatedPurchaseTotal` is decremented by the
  order's `netTotal`
- **Response 409**: order's `createdAt` is not the same calendar day as the void request
  (FR-027 same-day rule), or order is already `Voided` (FR-028 no double-void)
- **Response 404**: not found

## GET /sales-orders/pricing-preview

Preview discount calculation for the current cart before checkout (used by `web/` cart screen
for live totals, per research item 5 — the Domain pricing logic runs once, server-side).

- **Auth**: any authenticated staff
- **Query/body**: same `lines`/`memberId` shape as POST /sales-orders
- **Response 200**: `{ subtotalAmount, promotionDiscountAmount, memberDiscountAmount, netTotal }`
  (no persistence, no stock check beyond a non-blocking warning flag)
