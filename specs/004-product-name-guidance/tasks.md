---

description: "Task list template for feature implementation"
---

# Tasks: คำแนะนำการตั้งชื่อสินค้าให้ชัดเจน

**Input**: Design documents from `/specs/004-product-name-guidance/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md (no `contracts/` —
research.md item 1 explains why none is needed for this feature)

**Tests**: Not included — this is static presentation copy in `web/`, not Domain/Application
business logic, so Constitution Principle III's NON-NEGOTIABLE unit-test mandate does not apply
(plan.md Technical Context "Testing"), and the spec does not request tests for this change.
Validation is manual, via `quickstart.md`.

**Organization**: Single user story (US1, P1) — the entire feature is one small edit.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1)
- File paths are exact, relative to the repository root

---

## Phase 1: Setup

**None required.** research.md confirms no new dependency, no new project, no environment change
— the existing `web/` dev setup from 001-pos-system is sufficient.

## Phase 2: Foundational

**None required.** There is only one user story; nothing needs to be shared or blocked ahead of it.

---

## Phase 3: User Story 1 - ผู้ดูแลระบบเห็นคำแนะนำการตั้งชื่อเมื่อเพิ่ม/แก้ไขสินค้า (Priority: P1) 🎯 MVP

**Goal**: The product Name field, on both the "Add product" and "Edit product" dialogs, shows a
clear-naming example as a placeholder and a short always-visible helper line — guidance only,
never a validation rule (FR-001–FR-005).

**Independent Test**: As Admin, open `/stock` → "Add product": confirm the Name field shows a
placeholder example and a helper line is visible immediately. Open "Edit" on any existing product:
confirm the same helper line is visible even though the field is pre-filled. Save a product with
any name (including a deliberately vague one) and confirm it saves without error.

### Implementation for User Story 1

- [X] T001 [US1] In `web/src/components/ProductFormDialog.tsx`, on the existing Name field's
      `<InputText value={values.name} onChange={...} className="w-full rounded-md border
      border-gray-300 px-3 py-2 text-sm" required />` (currently no `placeholder`, no helper text —
      research.md item 2): add `placeholder="เช่น เงาะ"` (FR-001, shows only while the field is
      empty — standard placeholder behavior covers US1 scenario 1 and the edge case where the
      field is cleared during editing) and, directly beneath the `<InputText>` inside the same
      `<div className="space-y-1">`, add `<p className="text-xs text-gray-500">ตั้งชื่อสินค้าให้ชัดเจน
      เช่น "เงาะ", "มะม่วง" แทนคำกว้าง ๆ เช่น "ผลไม้", "สินค้า 1"</p>` — always rendered regardless of
      the field's current value, so it is visible on both Add (empty field) and Edit (pre-filled
      field) per US1 scenario 2 and 3 (FR-002, FR-003). Do not add any `onBlur`/`onChange`
      validation, error state, or submit-blocking logic tied to this text — saving must continue to
      accept any name unchanged (FR-004, FR-005).

**Checkpoint**: User Story 1 — and the entire feature — is complete and independently verifiable
via `quickstart.md`.

---

## Phase 4: Polish & Cross-Cutting Concerns

- [X] T002 [P] Run the `quickstart.md` validation walkthrough (all 3 steps) against a running
      `api/` + `web/`: confirm the placeholder and helper text render on both Add and Edit, and
      that saving a product with any name (specific or vague) still succeeds with no new
      rejection. Fix any discrepancy found.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup / Foundational**: Empty — nothing to do before Phase 3.
- **User Story 1 (Phase 3)**: No dependencies; the only task, T001, can start immediately.
- **Polish (Phase 4)**: Depends on T001 being complete (there is nothing to validate before then).

### Parallel Opportunities

- None within Phase 3 — a single task editing a single file.
- T002 (Polish) has no code-file conflict with anything else, but is logically sequential after
  T001 (nothing to validate until the edit exists) despite the `[P]` marker's usual meaning of
  "no dependency" — here it simply denotes "no other task competes for the same file."

---

## Implementation Strategy

### MVP First (and only) — User Story 1

1. Complete T001 (the entire feature is this one edit).
2. Complete T002 — run `quickstart.md` to confirm all 3 acceptance scenarios hold.
3. Done — no further phases exist for this feature.
