# Quickstart: Export รายงานสินค้าคงเหลือและประวัติการขาย

**Feature**: `002-export-reports-sales-history` | **Purpose**: End-to-end validation that both
export buttons work, walking through each user story from `spec.md` against the contracts in
`contracts/`.

## Prerequisites

- The base `001-pos-system` app already running and reachable (see that feature's own
  `quickstart.md` for first-time setup — API + PostgreSQL + `web/` dev server).
- Logged in as the seeded `Admin` account (`Cashier` accounts cannot see either Export button —
  that's part of what this quickstart checks).
- At least a few products in stock (some active, ideally one below its low-stock threshold) and a
  handful of sales orders spanning more than one day, including at least one `Voided` order — so
  the export has something non-trivial to prove.

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

### 1. Stock levels export (User Story 1, contracts/stock-export.md)

1. As Admin, open **Reports** and find the "Stock levels" section — confirm it lists the same
   products (and the same low-stock badges) you set up in Prerequisites.
2. Click **Export**. Confirm a `.xlsx` file downloads (not a JSON download, not an error toast).
3. Open the downloaded file in Excel (or any spreadsheet program). Confirm:
   - One row per product shown on-screen, same names/quantities/low-stock flags.
   - Any Thai product names render correctly (no mojibake).
4. Log out, log in as a `Cashier` account, and confirm the Reports screen — and therefore the
   Export button on it — isn't reachable at all (redirected to `/sales`, per the existing
   Admin-only guard this feature reuses). This proves FR-008/SC-005 without needing to call the API
   directly.

### 2. Sale history export, filtered (User Story 2, contracts/sales-history-export.md)

1. As Admin, open **Sale History**.
2. Set a date range that includes both a `Completed` and a `Voided` order, and leave Status = "All".
3. Click **Export**. Confirm a `.xlsx` file downloads.
4. Open it and confirm:
   - Row count matches the number of orders the on-screen table reports as `total`, **not** just
     the number of rows visible on the current page (test this concretely by picking a date range
     with more orders than the page size, and confirming the file has more rows than one page's
     worth — this proves FR-004).
   - Each row shows Date, Staff name, Member name (blank when not linked), Net total, and Status.
   - Both `Completed` and `Voided` orders appear, each correctly labeled.
5. Narrow the Status filter to `Voided` only, export again, and confirm the new file contains only
   voided orders.
6. Pick a date range with zero matching orders, export, and confirm the file still downloads
   successfully with just a header row (no error) — proves FR-009.

### 3. Export button behavior while a file is preparing (Edge Cases)

1. On either screen, click Export once and immediately try clicking it again before the download
   finishes. Confirm the button is disabled during that window (FR-010) rather than triggering a
   second overlapping download.

## What "done" looks like

All six checks above pass without opening browser dev tools or calling the API directly — a real
Admin user, using only the two Export buttons, can get both files and see them behave exactly as
`spec.md`'s acceptance scenarios describe.
