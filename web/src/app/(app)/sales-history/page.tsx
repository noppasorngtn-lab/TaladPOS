"use client";

import { useCallback, useEffect, useState } from "react";
import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { Button } from "primereact/button";
import { useAuth } from "@/lib/auth/AuthProvider";
import {
  searchSalesOrders,
  type SalesOrderSummary,
  type SalesOrderSearchFilters,
} from "@/lib/api/salesOrders";
import { ApiError } from "@/lib/api/client";

const emptyFilters: SalesOrderSearchFilters = { page: 1, pageSize: 20 };

// FR-026: search sale history by date range, staff, member, and status.
export default function SalesHistoryPage() {
  const { token } = useAuth();
  const [filters, setFilters] = useState<SalesOrderSearchFilters>(emptyFilters);
  const [orders, setOrders] = useState<SalesOrderSummary[]>([]);
  const [total, setTotal] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    if (!token) return;
    try {
      const result = await searchSalesOrders(token, filters);
      setOrders(result.items);
      setTotal(result.total);
      setError(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Unable to load sale history.");
    }
  }, [token, filters]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  return (
    <main className="flex flex-col gap-4 p-4">
      <h1 className="text-lg font-semibold text-gray-900">Sale history</h1>

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
            value={filters.from ?? ""}
            onChange={(e) => setFilters((f) => ({ ...f, from: e.target.value || undefined, page: 1 }))}
            className="rounded-md border border-gray-300 px-3 py-2 text-sm"
          />
        </div>
        <div className="space-y-1">
          <label className="block text-sm font-medium text-gray-700">To</label>
          <input
            type="date"
            value={filters.to ?? ""}
            onChange={(e) => setFilters((f) => ({ ...f, to: e.target.value || undefined, page: 1 }))}
            className="rounded-md border border-gray-300 px-3 py-2 text-sm"
          />
        </div>
        <div className="space-y-1">
          <label className="block text-sm font-medium text-gray-700">Status</label>
          <select
            value={filters.status ?? ""}
            onChange={(e) =>
              setFilters((f) => ({
                ...f,
                status: (e.target.value || undefined) as SalesOrderSearchFilters["status"],
                page: 1,
              }))
            }
            className="rounded-md border border-gray-300 px-3 py-2 text-sm"
          >
            <option value="">All</option>
            <option value="Completed">Completed</option>
            <option value="Voided">Voided</option>
          </select>
        </div>
        <Button
          type="button"
          label="Clear filters"
          onClick={() => setFilters(emptyFilters)}
          className="rounded-md border border-gray-300 px-3 py-2 text-sm text-gray-700"
        />
      </div>

      <DataTable
        value={orders}
        dataKey="id"
        className="w-full overflow-hidden rounded-lg border border-gray-200 bg-white text-sm"
      >
        <Column
          header="Date"
          body={(row: SalesOrderSummary) => new Date(row.createdAt).toLocaleString()}
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
        />
        <Column
          header="Net total"
          body={(row: SalesOrderSummary) => `฿${row.netTotal.toFixed(2)}`}
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
        />
        <Column
          header="Status"
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
          body={(row: SalesOrderSummary) =>
            row.status === "Voided" ? (
              <span className="rounded-full bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-600">
                Voided
              </span>
            ) : (
              <span className="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800">
                Completed
              </span>
            )
          }
        />
      </DataTable>

      <p className="text-sm text-gray-500">{total} total order(s)</p>
    </main>
  );
}
