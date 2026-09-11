# Implementation Plan: คำแนะนำการตั้งชื่อสินค้าให้ชัดเจน

**Branch**: `004-product-name-guidance` | **Date**: 2026-09-11 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-product-name-guidance/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Add UI-only naming guidance to the existing product Add/Edit form (`web/src/components/ProductFormDialog.tsx`,
shared by both `web/src/app/(app)/stock/page.tsx` flows): a placeholder example (e.g. "เงาะ") in
the empty Name field, plus a short static helper line always visible near the field explaining
that product names should be specific rather than generic. No backend change, no new validation,
no data migration — purely a two-line addition to one existing React component.

## Technical Context

**Language/Version**: TypeScript ^5 / Node.js 20 LTS (unchanged — frontend-only feature).

**Primary Dependencies**: None new. Reuses the existing `InputText` (PrimeReact, unstyled +
`tailwindcss-primeui`) and Tailwind CSS classes already used throughout
`web/src/components/ProductFormDialog.tsx`.

**Storage**: N/A — no data model change. `Product.Name` (existing field, `api/src/TaladPOS.Domain/Entities/Product.cs`)
is read/written exactly as before; this feature never touches the API or database.

**Testing**: No unit test obligation under Constitution Principle III — this is presentation-only
markup/copy in `web/`, not Domain/Application business logic. Validated via the `quickstart.md`
manual walkthrough; optionally spot-checked by extending an existing Playwright products spec
(`web/tests/products.spec.ts`) with one assertion that the placeholder/helper text renders, but
that is not required for a static-text change.

**Target Platform**: Same as 001/003 — Next.js frontend served to POS terminals/tablets and desktop.

**Project Type**: Web application (frontend-only change within the existing `web/` app — no new
project, no `api/` change).

**Performance Goals**: N/A — static text render, no measurable performance impact.

**Constraints**: Must not add, change, or bypass any existing validation on `Product.Name` (FR-004
— guidance only, never a rejection). Must not require any API/contract change (Constitution
Principle I is trivially satisfied since `web/` already owns this presentation-only text).

**Scale/Scope**: One existing file edited (`ProductFormDialog.tsx`); two small UI elements added
(a `placeholder` attribute, a static helper `<p>`); zero new files required for the core change.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. Clear API/Frontend Separation | No API call added or changed; the guidance text is static copy rendered entirely in `web/` | PASS |
| II. Domain-Driven Design for the API | Not applicable — no `api/` change of any kind | PASS |
| III. Test-First for Business Logic | Not applicable — no Domain/Application business logic added; this is presentation copy, not a business rule (FR-004 explicitly rules out new validation logic) | PASS |
| IV. Frontend Technology Standards | Change stays within existing Next.js + Tailwind CSS + PrimeReact (unstyled) conventions already used by `ProductFormDialog.tsx`; no new dependency | PASS |
| V. Repository & Folder Structure | Edit confined to `web/src/components/ProductFormDialog.tsx`; no cross-boundary import | PASS |

No violations — Complexity Tracking is not applicable (left empty below).

## Project Structure

### Documentation (this feature)

```text
specs/004-product-name-guidance/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

No `contracts/` directory — this feature adds no API endpoint and changes no existing contract
(research.md item 1).

### Source Code (repository root)

```text
web/
└── src/
    └── components/
        └── ProductFormDialog.tsx   # EDIT ONLY: add `placeholder` to the Name InputText (FR-001)
                                     #   and a static helper <p> under it (FR-002), used by both
                                     #   the "Add product" and "Edit product" flows on /stock
```

**Structure Decision**: No new directory, no new project. This is a single-file edit inside the
existing `web/` app from 001-pos-system, consistent with Constitution Principle V (all frontend
code stays under `web/`).

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
