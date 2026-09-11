# Data Model: คำแนะนำการตั้งชื่อสินค้าให้ชัดเจน

**Feature**: `004-product-name-guidance` | **Spec**: [spec.md](./spec.md)

## No entity changes

This feature adds no entity, field, or migration. `Product.Name` (documented in
`specs/001-pos-system/data-model.md` — `string`, required) is read and written exactly as it is
today by the existing `POST /products` / `PUT /products` endpoints (`specs/001-pos-system/contracts/products.md`).
FR-004/FR-005 explicitly rule out any new validation rule or retroactive rename of existing data.

The only "data" this feature introduces is presentation-only, static copy embedded in
`ProductFormDialog.tsx` (the placeholder example and the helper sentence) — not a persisted
entity, not configuration read from the database.
