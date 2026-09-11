# Contracts: Export รายงานสินค้าคงเหลือและประวัติการขาย

This feature adds exactly two new REST endpoints, both returning a binary Excel (.xlsx) file
instead of the JSON this API otherwise always returns. Both extend existing, already-documented
resources rather than introducing new ones:

- [`stock-export.md`](./stock-export.md) — extends the Reports resource (`GET /reports/...`)
- [`sales-history-export.md`](./sales-history-export.md) — extends the Sales Orders resource
  (`GET /sales-orders/...`)

## Shared conventions for both endpoints

- **Method**: `GET` (exporting is a read, not a mutation — safe to link/bookmark, though both
  require auth so a bare link won't work outside an authenticated session)
- **Auth**: Admin only (`[Authorize(Policy = "Admin")]`) — same policy already guarding the
  screens these exports live on
- **Success response**: `200 OK`
  - `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
  - `Content-Disposition: attachment; filename="<descriptive-name>.xlsx"`
  - Body: the raw `.xlsx` file bytes
- **Error responses**: same JSON `{ "error": { "code": "...", "message": "..." } }` shape as every
  other endpoint in this API — an export failure (e.g., unauthorized) does **not** return a
  malformed or empty file, it returns the normal JSON error with the normal status code (`401`,
  `403`, etc.), so the frontend's existing `ApiError` handling applies unchanged
- **Empty result set**: still `200 OK` with a valid `.xlsx` containing only the header row (FR-009)
  — never a `404` or an error for "no rows matched"
