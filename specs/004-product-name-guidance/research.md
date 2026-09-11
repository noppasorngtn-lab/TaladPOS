# Phase 0 Research: คำแนะนำการตั้งชื่อสินค้าให้ชัดเจน

**Feature**: `004-product-name-guidance` | **Spec**: [spec.md](./spec.md)

No unresolved `NEEDS CLARIFICATION` items — scope was already narrowed with the user before
`/speckit-specify` (guidance-only, no validation, no retroactive data change). Two small
integration decisions, found by reading the current code:

## 1. Why no `contracts/` directory for this feature

**Decision**: Skip `contracts/` entirely.

**Rationale**: FR-001–FR-005 are satisfied purely by static markup/copy inside
`web/src/components/ProductFormDialog.tsx`. `POST /products` and `PUT /products` (contracts/products.md,
001-pos-system) are unchanged — the same `name` field, same validation, same response shape.
There is no new or modified interface for `web/` and `api/` to agree on.

**Alternatives considered**: N/A — this is the plan template's documented skip condition ("skip if
project is purely internal" extended here to "no external interface changes at all").

## 2. Where the placeholder/helper text lives

**Decision**: Edit only `web/src/components/ProductFormDialog.tsx`'s Name field (around the
existing `<InputText value={values.name} ...>`, currently lines 85–93): add a `placeholder` prop
with an example name, and one static `<p>` helper line directly under the input, always rendered
(not conditional on empty/non-empty) so it also helps during Edit (where the field is pre-filled
and a placeholder alone would never be visible — spec.md US1 scenario 3).

**Rationale**: `ProductFormDialog` is already the single shared component for both the "Add
product" and "Edit product" flows on `/stock` (verified — `web/src/app/(app)/stock/page.tsx`
renders one `<ProductFormDialog product={editingProduct}>`, `editingProduct` undefined for Add).
Editing it once covers both flows FR-002 requires, with no new component and no prop-drilling.

**Alternatives considered**: A separate `<Tooltip>`/info-icon component — rejected as
over-engineering for one line of static copy; a `title` attribute instead of a visible `<p>` —
rejected because spec.md FR-003 requires the guidance visible without any hover/interaction.

## 3. Helper-text styling convention

**Decision**: `<p className="text-xs text-gray-500">...</p>`, matching the existing muted-caption
scale already used in this codebase (`ProductCard.tsx` uses `text-xs text-gray-400` for a
secondary caption; error messages use `text-sm text-red-600`). No prior "helper text under a
field" pattern exists yet in `web/`, so this picks the closest existing convention rather than
inventing a new color/size scale.

**Alternatives considered**: None needed — low-stakes styling choice, no accessibility or
functional trade-off either way.
