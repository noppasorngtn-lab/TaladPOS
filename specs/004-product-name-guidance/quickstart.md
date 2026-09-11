# Quickstart: คำแนะนำการตั้งชื่อสินค้าให้ชัดเจน

**Feature**: `004-product-name-guidance` | **Purpose**: End-to-end validation that the naming
guidance is visible on both the Add and Edit product forms, and that it never blocks saving.

## Prerequisites

- `api/` running (`dotnet run --project src/TaladPOS.Api`) — unchanged by this feature, needed
  only because `/stock` requires a live API session (existing 001-pos-system behavior).
- `web/` running (`npm run dev`).
- An Admin session (`/stock` is Admin-only — existing FR-009–FR-013 guard, unchanged).

## Run

```bash
cd api && dotnet run --project src/TaladPOS.Api
cd web && npm run dev
```

Log in as Admin, open `/stock`.

## Validation Walkthrough

### 1. Add product — placeholder visible (US1 scenario 1, FR-001, FR-003)

1. Click "Add product".
2. Confirm the Name field is empty and shows a placeholder example of a clear product name (e.g.
   "เงาะ") with no extra click needed.

### 2. Add/Edit product — helper text visible (US1 scenario 2, FR-002, FR-003)

1. On the same "Add product" dialog, confirm a short helper line is visible near the Name field
   explaining that names should be specific (not a generic term).
2. Close the dialog. Open "Edit" on any existing product (including one with a vague name, if one
   exists — e.g. a fixture product literally named something generic).
3. Confirm the same helper line is visible here too, even though the Name field is pre-filled with
   the existing value (US1 scenario 3).

### 3. Guidance never blocks saving (US1 scenario 4, FR-004, FR-005)

1. In either the Add or Edit dialog, type any name — including a deliberately vague one (e.g.
   "สินค้า1") — into the Name field.
2. Click Save. Confirm it saves successfully with no error or rejection related to the name's
   specificity (only pre-existing validation, e.g. empty name, still applies — unchanged).
3. Confirm no existing product's name changed on its own anywhere else in the system (FR-005 — the
   feature never rewrites existing data).

## Success Criteria Mapping

| Quickstart step | Success Criteria |
|---|---|
| 1, 2 | SC-001, SC-002 |
| 3 | SC-003 |
