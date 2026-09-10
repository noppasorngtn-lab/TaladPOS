"use client";

import { useCallback, useEffect, useState } from "react";
import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { Button } from "primereact/button";
import { InputText } from "primereact/inputtext";
import { useAuth } from "@/lib/auth/AuthProvider";
import {
  searchProducts,
  getLowStockProducts,
  getProduct,
  deleteProduct,
  type ProductSummary,
  type ProductDetail,
} from "@/lib/api/products";
import { ApiError } from "@/lib/api/client";
import { ProductFormDialog } from "@/components/ProductFormDialog";

export default function StockPage() {
  const { token } = useAuth();
  const [search, setSearch] = useState("");
  const [products, setProducts] = useState<ProductSummary[]>([]);
  const [lowStockCount, setLowStockCount] = useState(0);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<ProductDetail | undefined>(undefined);
  const [error, setError] = useState<string | null>(null);

  // FR-012/FR-013: the stock list and the low-stock indicator both read from the
  // Admin-only product endpoints (contracts/products.md GET /products, GET /products/low-stock).
  const refresh = useCallback(async () => {
    if (!token) return;
    try {
      const [list, lowStock] = await Promise.all([
        searchProducts(token, search, true),
        getLowStockProducts(token),
      ]);
      setProducts(list.items);
      setLowStockCount(lowStock.total);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Unable to load products.");
    }
  }, [token, search]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  function openAddDialog() {
    setEditingProduct(undefined);
    setIsDialogOpen(true);
  }

  async function openEditDialog(product: ProductSummary) {
    if (!token) return;
    try {
      const detail = await getProduct(token, product.id);
      setEditingProduct(detail);
      setIsDialogOpen(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Unable to load this product.");
    }
  }

  // FR-011: soft delete only — the product disappears from sale but its sales history is untouched.
  async function handleDelete(product: ProductSummary) {
    if (!token) return;
    if (!window.confirm(`Remove "${product.name}" from sale? Its sales history will be kept.`)) {
      return;
    }
    try {
      await deleteProduct(token, product.id);
      await refresh();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Unable to remove this product.");
    }
  }

  return (
    <main className="flex flex-col gap-4 p-4">
      <div className="flex items-center justify-between">
        <h1 className="text-lg font-semibold text-gray-900">Stock</h1>
        <Button
          type="button"
          label="Add product"
          onClick={openAddDialog}
          className="rounded-md bg-gray-900 px-4 py-2 text-sm font-medium text-white"
        />
      </div>

      {lowStockCount > 0 && (
        <div className="rounded-md border border-amber-200 bg-amber-50 px-4 py-2 text-sm text-amber-800">
          {lowStockCount} product{lowStockCount === 1 ? "" : "s"} at or below its low-stock
          threshold.
        </div>
      )}

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">
          {error}
        </div>
      )}

      <InputText
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder="Search by name or barcode..."
        className="w-full max-w-sm rounded-md border border-gray-300 px-3 py-2 text-sm"
      />

      <DataTable
        value={products}
        dataKey="id"
        className="w-full overflow-hidden rounded-lg border border-gray-200 bg-white text-sm"
      >
        <Column
          field="name"
          header="Name"
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
        />
        <Column
          header="Price"
          body={(row: ProductSummary) => `฿${row.price.toFixed(2)}`}
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
        />
        <Column
          field="quantityOnHand"
          header="Qty on hand"
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
        />
        <Column
          header="Barcode"
          body={(row: ProductSummary) => row.barcode ?? "-"}
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
        />
        <Column
          header="Status"
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
          body={(row: ProductSummary) =>
            row.lowStock ? (
              <span className="rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-800">
                Low stock
              </span>
            ) : null
          }
        />
        <Column
          header="Actions"
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
          body={(row: ProductSummary) => (
            <div className="flex gap-2">
              <Button
                type="button"
                label="Edit"
                onClick={() => openEditDialog(row)}
                className="rounded-md border border-gray-300 px-2 py-1 text-xs text-gray-700"
              />
              <Button
                type="button"
                label="Remove"
                onClick={() => handleDelete(row)}
                className="rounded-md border border-red-300 px-2 py-1 text-xs text-red-600"
              />
            </div>
          )}
        />
      </DataTable>

      <ProductFormDialog
        visible={isDialogOpen}
        onHide={() => setIsDialogOpen(false)}
        onSaved={() => refresh()}
        product={editingProduct}
      />
    </main>
  );
}
