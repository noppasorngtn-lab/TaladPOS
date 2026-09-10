# Data Model: ระบบ POS สำหรับร้านขายผลไม้และสินค้าทั่วไป (Single Store)

**Feature**: `001-pos-system` | **Date**: 2026-09-10

Entities below map 1:1 to the Key Entities in `spec.md` and are owned by the API's Domain layer
(Constitution Principle II). Field names are illustrative for planning; exact casing/types are
decided during implementation.

## Product

Represents a sellable item in the single store's catalog.

| Field | Type | Rules |
|---|---|---|
| Id | Guid/int (PK) | System-generated |
| Name | string | Required |
| ImageUrl | string | Optional; relative path under API static files (research item 3) |
| Barcode | string | Optional; **unique when present** (FR-014) |
| Price | decimal | Required; > 0 |
| QuantityOnHand | int | Required; >= 0; decremented on sale, restored on void |
| LowStockThreshold | int | Optional; defaults to a system-wide default if unset (spec Assumptions) |
| IsActive | bool | false when "deleted" from sale (FR-011) — soft delete so historical `SalesOrderLine` rows keep valid references |
| RowVersion | concurrency token | Backs the optimistic-concurrency stock guard (research item 4) |

**Validation rules**: `Price > 0`; `QuantityOnHand >= 0` (never allowed to go negative — enforced
by the same transaction that creates a `SalesOrderLine`); `Barcode` unique index (nullable-safe).

**State transitions**: `IsActive: true → false` (soft delete, one-way in this version — no
"undelete" requirement in spec).

## Staff

Represents a user account that can log in to the sales screen and, for admins, back-office
screens.

| Field | Type | Rules |
|---|---|---|
| Id | Guid/int (PK) | System-generated |
| Name | string | Required |
| Username | string | Required; unique |
| PasswordHash | string | Required; never exposed via API |
| Role | enum (`Cashier`, `Admin`) | Required (FR-034) |
| IsActive | bool | Disables login without deleting sales history attribution |

**Validation rules**: `Username` unique; password stored only as a hash (bcrypt/PBKDF2 — decided
at implementation time, not a domain concern).

## Member

Represents a loyalty program participant.

| Field | Type | Rules |
|---|---|---|
| Id | Guid/int (PK) | System-generated |
| Name | string | Required |
| PhoneNumber | string | Required; **unique** (FR-014→FR-016 dedupe rule) |
| AccumulatedPurchaseTotal | decimal | Starts at 0; increased by each linked, paid `SalesOrder`'s net total (FR-018); decreased if that order is later voided |
| JoinedAt | datetime | System-generated on signup |

**Validation rules**: `PhoneNumber` unique index; `AccumulatedPurchaseTotal >= 0`.

## Promotion

Represents a time-boxed percentage discount rule.

| Field | Type | Rules |
|---|---|---|
| Id | Guid/int (PK) | System-generated |
| Scope | enum (`PerProduct`, `WholeBill`, `MemberDiscount`) | Required (FR-020, FR-021) |
| ProductId | FK to Product, nullable | Required when `Scope = PerProduct`; null otherwise |
| DiscountPercent | decimal | Required; 0 < value <= 100 |
| StartDate | date | Required |
| EndDate | date | Required; >= StartDate |
| IsActive | bool | Allows disabling without deleting history of what applied to past orders |

**Validation rules**: `EndDate >= StartDate`; `ProductId` required iff `Scope = PerProduct`
(mutually exclusive with `WholeBill`/`MemberDiscount`); a promotion only applies to a sale when
the sale's date falls within `[StartDate, EndDate]` (FR-023).

## SalesOrder

Represents one completed (or voided) sales transaction — the aggregate root for a sale.

| Field | Type | Rules |
|---|---|---|
| Id | Guid/int (PK) | System-generated |
| CreatedAt | datetime | Required; the date used for promotion-window checks and void-window checks |
| StaffId | FK to Staff | Required (FR-008) |
| MemberId | FK to Member, nullable | Optional (FR-025) |
| Status | enum (`Completed`, `Voided`) | Starts `Completed`; one-way transition to `Voided` (FR-027/FR-028) |
| SubtotalAmount | decimal | Sum of line amounts before discount |
| PromotionDiscountAmount | decimal | Total from `PerProduct`/`WholeBill` promotions |
| MemberDiscountAmount | decimal | Total from the member-specific discount, applied to the post-promotion remainder (FR-036) |
| NetTotal | decimal | `SubtotalAmount - PromotionDiscountAmount - MemberDiscountAmount`, floored at 0 |
| VoidedAt | datetime, nullable | Set when `Status` transitions to `Voided` |

**Validation rules**: `NetTotal >= 0` (FR-036); a void transition is only permitted when
`CreatedAt` is the same calendar day as the void request (FR-027) and only from `Completed` (not
already `Voided`) (FR-028).

**State transitions**: `Completed → Voided` (one-way, same-day only, restores stock on every
`SalesOrderLine`, reverses the `Member.AccumulatedPurchaseTotal` credit if a member was linked).

## SalesOrderLine

Represents one product line within a `SalesOrder`.

| Field | Type | Rules |
|---|---|---|
| Id | Guid/int (PK) | System-generated |
| SalesOrderId | FK to SalesOrder | Required |
| ProductId | FK to Product | Required; kept even if the `Product` is later soft-deleted (FR-011) |
| ProductNameSnapshot | string | Captured at sale time so history is unaffected by later product edits |
| UnitPriceSnapshot | decimal | Captured at sale time |
| Quantity | int | Required; > 0 |
| LineDiscountAmount | decimal | Portion of `PromotionDiscountAmount` attributable to this line, if a `PerProduct` promotion applied |

**Validation rules**: `Quantity > 0` (rejects the "0 or negative quantity" edge case);
`ProductNameSnapshot`/`UnitPriceSnapshot` are immutable once written.

## Relationships

```text
Staff (1) ──< (many) SalesOrder
Member (0..1) ──< (many) SalesOrder
SalesOrder (1) ──< (many) SalesOrderLine
Product (1) ──< (many) SalesOrderLine
Product (0..1) ──< (many) Promotion   [only when Promotion.Scope = PerProduct]
```

## Notes for Phase 1 Contracts

- All monetary fields are `decimal`, never floating point.
- `SalesOrder` creation, void, and stock decrement/restore are each a single Domain-layer
  operation wrapped in one database transaction (research items 4–5) — the API never exposes a
  way to update `SalesOrderLine`/`Product.QuantityOnHand` directly outside of these operations.
