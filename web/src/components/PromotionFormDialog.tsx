"use client";

import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "primereact/dialog";
import { InputText } from "primereact/inputtext";
import { Button } from "primereact/button";
import { useAuth } from "@/lib/auth/AuthProvider";
import { searchProducts, type ProductSummary } from "@/lib/api/products";
import {
  createPromotion,
  updatePromotion,
  type Promotion,
  type PromotionFormValues,
  type PromotionScope,
} from "@/lib/api/promotions";
import { ApiError } from "@/lib/api/client";

interface PromotionFormDialogProps {
  visible: boolean;
  onHide: () => void;
  onSaved: (promotion: Promotion) => void;
  /** Present when editing an existing promotion; absent when adding a new one. */
  promotion?: Promotion;
}

const emptyForm: PromotionFormValues = {
  scope: "PerProduct",
  productId: "",
  discountPercent: 10,
  startDate: "",
  endDate: "",
};

function toFormValues(promotion?: Promotion): PromotionFormValues {
  if (!promotion) return emptyForm;
  return {
    scope: promotion.scope,
    productId: promotion.productId ?? "",
    discountPercent: promotion.discountPercent,
    startDate: promotion.startDate,
    endDate: promotion.endDate,
  };
}

// FR-020–FR-022: scope (per-product / whole-bill / member discount), discount %, date range.
export function PromotionFormDialog({ visible, onHide, onSaved, promotion }: PromotionFormDialogProps) {
  const { token } = useAuth();
  const [values, setValues] = useState<PromotionFormValues>(() => toFormValues(promotion));
  const [products, setProducts] = useState<ProductSummary[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setValues(toFormValues(promotion));
    setError(null);
  }, [promotion, visible]);

  useEffect(() => {
    if (!token || !visible) return;
    searchProducts(token, "").then((result) => setProducts(result.items));
  }, [token, visible]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!token) return;
    setIsSubmitting(true);
    setError(null);
    try {
      const saved = promotion
        ? await updatePromotion(token, promotion.id, values)
        : await createPromotion(token, values);
      onSaved(saved);
      onHide();
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "Unable to save this promotion. Please try again.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Dialog
      header={promotion ? "Edit promotion" : "Add promotion"}
      visible={visible}
      onHide={onHide}
      className="w-full max-w-md rounded-lg bg-white p-4 shadow-lg"
    >
      <form onSubmit={handleSubmit} className="space-y-3">
        <div className="space-y-1">
          <label className="text-sm font-medium text-gray-700">Scope</label>
          <select
            value={values.scope}
            onChange={(e) =>
              setValues((v) => ({ ...v, scope: e.target.value as PromotionScope, productId: "" }))
            }
            className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
          >
            <option value="PerProduct">Per product</option>
            <option value="WholeBill">Whole bill</option>
            <option value="MemberDiscount">Member discount</option>
          </select>
        </div>

        {values.scope === "PerProduct" && (
          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Product</label>
            <select
              value={values.productId}
              onChange={(e) => setValues((v) => ({ ...v, productId: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              required
            >
              <option value="">Select a product...</option>
              {products.map((product) => (
                <option key={product.id} value={product.id}>
                  {product.name}
                </option>
              ))}
            </select>
          </div>
        )}

        <div className="space-y-1">
          <label className="text-sm font-medium text-gray-700">Discount percent</label>
          <InputText
            type="number"
            min={0.01}
            max={100}
            step="0.01"
            value={String(values.discountPercent)}
            onChange={(e) => setValues((v) => ({ ...v, discountPercent: Number(e.target.value) }))}
            className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
            required
          />
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Start date</label>
            <input
              type="date"
              value={values.startDate}
              onChange={(e) => setValues((v) => ({ ...v, startDate: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              required
            />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">End date</label>
            <input
              type="date"
              value={values.endDate}
              onChange={(e) => setValues((v) => ({ ...v, endDate: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              required
            />
          </div>
        </div>

        {error && <p className="text-sm text-red-600">{error}</p>}

        <div className="flex justify-end gap-2 pt-2">
          <Button
            type="button"
            label="Cancel"
            onClick={onHide}
            className="rounded-md px-3 py-1.5 text-sm text-gray-600"
          />
          <Button
            type="submit"
            label={isSubmitting ? "Saving..." : "Save"}
            disabled={isSubmitting}
            className="rounded-md bg-gray-900 px-4 py-1.5 text-sm font-medium text-white disabled:opacity-50"
          />
        </div>
      </form>
    </Dialog>
  );
}
