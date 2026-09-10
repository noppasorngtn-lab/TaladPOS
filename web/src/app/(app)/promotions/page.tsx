"use client";

import { useCallback, useEffect, useState } from "react";
import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { Button } from "primereact/button";
import { useAuth } from "@/lib/auth/AuthProvider";
import { searchProducts, type ProductSummary } from "@/lib/api/products";
import {
  listPromotions,
  deactivatePromotion,
  type Promotion,
} from "@/lib/api/promotions";
import { ApiError } from "@/lib/api/client";
import { PromotionFormDialog } from "@/components/PromotionFormDialog";

const scopeLabels: Record<Promotion["scope"], string> = {
  PerProduct: "Per product",
  WholeBill: "Whole bill",
  MemberDiscount: "Member discount",
};

export default function PromotionsPage() {
  const { token } = useAuth();
  const [promotions, setPromotions] = useState<Promotion[]>([]);
  const [productsById, setProductsById] = useState<Record<string, ProductSummary>>({});
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingPromotion, setEditingPromotion] = useState<Promotion | undefined>(undefined);
  const [error, setError] = useState<string | null>(null);

  // FR-020–FR-022: lists every promotion (past/active/future) so admins can see full history.
  const refresh = useCallback(async () => {
    if (!token) return;
    try {
      const [promotionList, productList] = await Promise.all([
        listPromotions(token),
        searchProducts(token, "", true),
      ]);
      setPromotions(promotionList.items);
      setProductsById(Object.fromEntries(productList.items.map((p) => [p.id, p])));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Unable to load promotions.");
    }
  }, [token]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  function openAddDialog() {
    setEditingPromotion(undefined);
    setIsDialogOpen(true);
  }

  function openEditDialog(promotion: Promotion) {
    setEditingPromotion(promotion);
    setIsDialogOpen(true);
  }

  async function handleDeactivate(promotion: Promotion) {
    if (!token) return;
    if (!window.confirm("Deactivate this promotion? Past sales that used it are unaffected.")) {
      return;
    }
    try {
      await deactivatePromotion(token, promotion.id);
      await refresh();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Unable to deactivate this promotion.");
    }
  }

  return (
    <main className="flex flex-col gap-4 p-4">
      <div className="flex items-center justify-between">
        <h1 className="text-lg font-semibold text-gray-900">Promotions</h1>
        <Button
          type="button"
          label="Add promotion"
          onClick={openAddDialog}
          className="rounded-md bg-gray-900 px-4 py-2 text-sm font-medium text-white"
        />
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">
          {error}
        </div>
      )}

      <DataTable
        value={promotions}
        dataKey="id"
        className="w-full overflow-hidden rounded-lg border border-gray-200 bg-white text-sm"
      >
        <Column
          header="Scope"
          body={(row: Promotion) => scopeLabels[row.scope]}
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
        />
        <Column
          header="Product"
          body={(row: Promotion) => (row.productId ? (productsById[row.productId]?.name ?? row.productId) : "-")}
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
        />
        <Column
          header="Discount"
          body={(row: Promotion) => `${row.discountPercent}%`}
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
        />
        <Column
          field="startDate"
          header="Start date"
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
        />
        <Column
          field="endDate"
          header="End date"
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
        />
        <Column
          header="Status"
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
          body={(row: Promotion) =>
            row.isActive ? (
              <span className="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800">
                Active
              </span>
            ) : (
              <span className="rounded-full bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-600">
                Deactivated
              </span>
            )
          }
        />
        <Column
          header="Actions"
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
          body={(row: Promotion) => (
            <div className="flex gap-2">
              <Button
                type="button"
                label="Edit"
                onClick={() => openEditDialog(row)}
                className="rounded-md border border-gray-300 px-2 py-1 text-xs text-gray-700"
              />
              {row.isActive && (
                <Button
                  type="button"
                  label="Deactivate"
                  onClick={() => handleDeactivate(row)}
                  className="rounded-md border border-red-300 px-2 py-1 text-xs text-red-600"
                />
              )}
            </div>
          )}
        />
      </DataTable>

      <PromotionFormDialog
        visible={isDialogOpen}
        onHide={() => setIsDialogOpen(false)}
        onSaved={() => refresh()}
        promotion={editingPromotion}
      />
    </main>
  );
}
