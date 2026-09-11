/**
 * Triggers a browser download of an in-memory Blob (research.md item 3, feature
 * 002-export-reports-sales-history) — used for the two Excel export endpoints, whose responses
 * are fetched via `apiFetchBlob` rather than navigated to directly.
 */
export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}
