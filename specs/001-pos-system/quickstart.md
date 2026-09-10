# Quickstart: ระบบ POS สำหรับร้านขายผลไม้และสินค้าทั่วไป (Single Store)

**Feature**: `001-pos-system` | **Purpose**: End-to-end validation that the feature works,
walking through each user story from `spec.md` against the contracts in `contracts/` and the
data model in `data-model.md`.

## Prerequisites

- PostgreSQL instance reachable by `api/` (connection string in `api/appsettings.Development.json`
  or environment variables — set up during `/speckit-tasks` implementation).
- `api/` solution built and EF Core migrations applied (`dotnet ef database update`).
- `web/` dependencies installed (`npm install`) with `NEXT_PUBLIC_API_BASE_URL` pointing at the
  running API.
- A seeded `Admin` staff account (seed script/migration created during implementation) — this
  quickstart cannot bootstrap the very first login itself.

## Run

```bash
# Terminal 1 — API
cd api
dotnet run --project src/TaladPOS.Api

# Terminal 2 — Frontend
cd web
npm run dev
```

Open the frontend URL (default `http://localhost:3000`).

## Validation Walkthrough

Each step references the acceptance scenario it proves and the contract endpoint(s) exercised.

### 1. Staff login (User Story 1, contracts/auth.md)

1. Log in as the seeded Admin via username/password.
2. Confirm the sales screen is inaccessible before login (spec.md US1 scenario 4) and accessible
   after.

### 2. Stock setup (User Story 2, contracts/products.md)

1. As Admin, add a product "มะม่วง" with an image, price, `quantityOnHand: 20`, and a
   `lowStockThreshold`.
2. Edit its price; confirm the change reflects immediately on the sales screen without altering
   any existing sales history (there is none yet).
3. Lower `quantityOnHand` (via a small test sale below, or directly for this check) to at or
   below the threshold; confirm it appears in `GET /products/low-stock`.

### 3. Core sale + stock cut (User Story 1, contracts/sales-orders.md)

1. As a Cashier (or Admin), search "มะม่วง" on the sales screen, add 3 to the cart, check out.
2. Confirm: `SalesOrder` created with `staffId` = the logged-in user; `Product.quantityOnHand`
   for มะม่วง drops from 20 to 17 (spec.md US1 scenario 1); `netTotal` matches price × 3 (no
   discounts active yet).
3. Repeat with a barcode lookup instead of name search (US1 scenario 2).
4. Try to check out a quantity greater than `quantityOnHand`; confirm `409` and no partial
   order is created (US1 edge case, FR-005).

### 4. Membership (User Story 3, contracts/members.md)

1. Sign up a member with a phone number and name; confirm `accumulatedPurchaseTotal: 0`.
2. Attempt to sign up the same phone number again; confirm `422` (FR-016).
3. Search the member by phone during a new checkout, link the order to them, complete checkout;
   confirm `accumulatedPurchaseTotal` increased by that order's `netTotal`.
4. Search a phone number that does not exist; confirm `404` and that checkout can still proceed
   without a member.

### 5. Promotions (User Story 4, contracts/promotions.md + sales-orders.md)

1. Create a `PerProduct` promotion on มะม่วง, 10%, `startDate`/`endDate` covering today.
2. Sell มะม่วง; confirm the line/order discount reflects 10% off (US4 scenario 1).
3. Create a `WholeBill` promotion with a future `startDate`; sell an eligible product today;
   confirm the discount is NOT applied (US4 scenario 2).
4. Repeat with an already-expired promotion; confirm it is NOT applied (US4 scenario 3).
5. Sell to a linked member while a promotion is active; confirm `promotionDiscountAmount` and
   `memberDiscountAmount` are both present and itemized separately, computed sequentially per
   FR-036 (US4 scenario 4, data-model.md `SalesOrder`).

### 6. Void (User Story 1 scenario 5, contracts/sales-orders.md)

1. Void a sales order created earlier today; confirm stock is restored, `status` becomes
   `Voided`, and (if a member was linked) their `accumulatedPurchaseTotal` decreases back.
2. Attempt to void the same order again; confirm `409` (FR-028).
3. Confirm the voided order no longer contributes to `/reports/sales-summary` or
   `/reports/best-sellers` (FR-033).

### 7. History & Reports (User Story 5, contracts/sales-orders.md + reports.md)

1. `GET /sales-orders` filtered by today's date range; confirm all non-voided orders from steps
   3–5 appear with correct line/discount/staff/member detail (FR-025, FR-026).
2. `GET /reports/sales-summary?granularity=daily`; confirm today's total matches the sum of
   completed orders' `netTotal`.
3. `GET /reports/best-sellers`; confirm มะม่วง ranks by quantity sold.
4. `GET /reports/sales-by-staff`; confirm the logged-in staff's total matches their orders.
5. `GET /reports/stock-levels`; confirm current `quantityOnHand` matches what step 3/6 left it at.

### 8. Reliability (contracts/sales-orders.md, FR-006/SC-007)

1. Stop the API mid-session (or block network to it) and attempt a checkout from the sales
   screen; confirm the frontend surfaces an error and does not show the order as completed, and
   no partial `SalesOrder` exists once the API is restarted.

## Success Criteria Mapping

| Quickstart step | Success Criteria |
|---|---|
| 3 | SC-001, SC-002 |
| 3 (search) | SC-003 |
| 5 | SC-005 |
| 2 | SC-006 |
| 7 | SC-004 |
| 8 | SC-007 |
