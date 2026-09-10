import { apiFetch } from "@/lib/api/client";

export interface SalesSummaryItem {
  period: string;
  totalSales: number;
  orderCount: number;
}

export interface BestSeller {
  productId: string;
  productName: string;
  quantitySold: number;
  totalAmount: number;
}

export interface StaffSales {
  staffId: string;
  staffName: string;
  orderCount: number;
  totalSales: number;
}

export interface StockLevel {
  productId: string;
  productName: string;
  quantityOnHand: number;
  lowStock: boolean;
}

// contracts/reports.md GET /reports/sales-summary (FR-029)
export function getSalesSummary(
  token: string,
  granularity: "daily" | "monthly",
  from: string,
  to: string,
): Promise<{ items: SalesSummaryItem[] }> {
  return apiFetch(`/reports/sales-summary?granularity=${granularity}&from=${from}&to=${to}`, { token });
}

// contracts/reports.md GET /reports/best-sellers (FR-030)
export function getBestSellers(token: string, from: string, to: string): Promise<{ items: BestSeller[] }> {
  return apiFetch(`/reports/best-sellers?from=${from}&to=${to}`, { token });
}

// contracts/reports.md GET /reports/sales-by-staff (FR-031)
export function getSalesByStaff(token: string, from: string, to: string): Promise<{ items: StaffSales[] }> {
  return apiFetch(`/reports/sales-by-staff?from=${from}&to=${to}`, { token });
}

// contracts/reports.md GET /reports/stock-levels (FR-032)
export function getStockLevels(token: string): Promise<{ items: StockLevel[] }> {
  return apiFetch("/reports/stock-levels", { token });
}
