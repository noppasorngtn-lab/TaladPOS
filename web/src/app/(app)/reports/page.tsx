"use client";

import { useCallback, useEffect, useState } from "react";
import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { useAuth } from "@/lib/auth/AuthProvider";
import {
  getSalesSummary,
  getBestSellers,
  getSalesByStaff,
  getStockLevels,
  type SalesSummaryItem,
  type BestSeller,
  type StaffSales,
  type StockLevel,
} from "@/lib/api/reports";
import { ApiError } from "@/lib/api/client";

const headerClassName = "border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700";
const bodyClassName = "border-b border-gray-100 px-4 py-2";

// toISOString() converts to UTC, which shifts the date for users east of UTC (e.g. Thailand,
// UTC+7) before 07:00 local time — build the "YYYY-MM-DD" string from local date parts instead.
function toLocalDateString(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function firstOfMonth(): string {
  const now = new Date();
  return toLocalDateString(new Date(now.getFullYear(), now.getMonth(), 1));
}

function today(): string {
  return toLocalDateString(new Date());
}

// FR-029–FR-032: daily/monthly sales, best-sellers, per-staff sales, and current stock levels —
// all excluding Voided orders (FR-033, enforced server-side).
export default function ReportsPage() {
  const { token } = useAuth();
  const [from, setFrom] = useState(firstOfMonth());
  const [to, setTo] = useState(today());
  const [granularity, setGranularity] = useState<"daily" | "monthly">("daily");
  const [salesSummary, setSalesSummary] = useState<SalesSummaryItem[]>([]);
  const [bestSellers, setBestSellers] = useState<BestSeller[]>([]);
  const [staffSales, setStaffSales] = useState<StaffSales[]>([]);
  const [stockLevels, setStockLevels] = useState<StockLevel[]>([]);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    if (!token) return;
    try {
      const [summary, sellers, byStaff, stock] = await Promise.all([
        getSalesSummary(token, granularity, from, to),
        getBestSellers(token, from, to),
        getSalesByStaff(token, from, to),
        getStockLevels(token),
      ]);
      setSalesSummary(summary.items);
      setBestSellers(sellers.items);
      setStaffSales(byStaff.items);
      setStockLevels(stock.items);
      setError(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Unable to load reports.");
    }
  }, [token, from, to, granularity]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  return (
    <main className="flex flex-col gap-6 p-4">
      <h1 className="text-lg font-semibold text-gray-900">Reports</h1>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">
          {error}
        </div>
      )}

      <div className="flex flex-wrap items-end gap-3">
        <div className="space-y-1">
          <label className="block text-sm font-medium text-gray-700">From</label>
          <input
            type="date"
            value={from}
            onChange={(e) => setFrom(e.target.value)}
            className="rounded-md border border-gray-300 px-3 py-2 text-sm"
          />
        </div>
        <div className="space-y-1">
          <label className="block text-sm font-medium text-gray-700">To</label>
          <input
            type="date"
            value={to}
            onChange={(e) => setTo(e.target.value)}
            className="rounded-md border border-gray-300 px-3 py-2 text-sm"
          />
        </div>
        <div className="space-y-1">
          <label className="block text-sm font-medium text-gray-700">Granularity</label>
          <select
            value={granularity}
            onChange={(e) => setGranularity(e.target.value as "daily" | "monthly")}
            className="rounded-md border border-gray-300 px-3 py-2 text-sm"
          >
            <option value="daily">Daily</option>
            <option value="monthly">Monthly</option>
          </select>
        </div>
      </div>

      <section className="space-y-2">
        <h2 className="text-sm font-semibold text-gray-900">Sales summary</h2>
        <DataTable value={salesSummary} dataKey="period" className="w-full overflow-hidden rounded-lg border border-gray-200 bg-white text-sm">
          <Column field="period" header="Period" headerClassName={headerClassName} bodyClassName={bodyClassName} />
          <Column
            header="Total sales"
            body={(row: SalesSummaryItem) => `฿${row.totalSales.toFixed(2)}`}
            headerClassName={headerClassName}
            bodyClassName={bodyClassName}
          />
          <Column field="orderCount" header="Orders" headerClassName={headerClassName} bodyClassName={bodyClassName} />
        </DataTable>
      </section>

      <section className="space-y-2">
        <h2 className="text-sm font-semibold text-gray-900">Best sellers</h2>
        <DataTable value={bestSellers} dataKey="productId" className="w-full overflow-hidden rounded-lg border border-gray-200 bg-white text-sm">
          <Column field="productName" header="Product" headerClassName={headerClassName} bodyClassName={bodyClassName} />
          <Column field="quantitySold" header="Qty sold" headerClassName={headerClassName} bodyClassName={bodyClassName} />
          <Column
            header="Total amount"
            body={(row: BestSeller) => `฿${row.totalAmount.toFixed(2)}`}
            headerClassName={headerClassName}
            bodyClassName={bodyClassName}
          />
        </DataTable>
      </section>

      <section className="space-y-2">
        <h2 className="text-sm font-semibold text-gray-900">Sales by staff</h2>
        <DataTable value={staffSales} dataKey="staffId" className="w-full overflow-hidden rounded-lg border border-gray-200 bg-white text-sm">
          <Column field="staffName" header="Staff" headerClassName={headerClassName} bodyClassName={bodyClassName} />
          <Column field="orderCount" header="Orders" headerClassName={headerClassName} bodyClassName={bodyClassName} />
          <Column
            header="Total sales"
            body={(row: StaffSales) => `฿${row.totalSales.toFixed(2)}`}
            headerClassName={headerClassName}
            bodyClassName={bodyClassName}
          />
        </DataTable>
      </section>

      <section className="space-y-2">
        <h2 className="text-sm font-semibold text-gray-900">Stock levels</h2>
        <DataTable value={stockLevels} dataKey="productId" className="w-full overflow-hidden rounded-lg border border-gray-200 bg-white text-sm">
          <Column field="productName" header="Product" headerClassName={headerClassName} bodyClassName={bodyClassName} />
          <Column field="quantityOnHand" header="Qty on hand" headerClassName={headerClassName} bodyClassName={bodyClassName} />
          <Column
            header="Status"
            headerClassName={headerClassName}
            bodyClassName={bodyClassName}
            body={(row: StockLevel) =>
              row.lowStock ? (
                <span className="rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-800">
                  Low stock
                </span>
              ) : null
            }
          />
        </DataTable>
      </section>
    </main>
  );
}
