"use client";

import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "primereact/dialog";
import { InputText } from "primereact/inputtext";
import { Button } from "primereact/button";
import { useAuth } from "@/lib/auth/AuthProvider";
import {
  createProduct,
  updateProduct,
  type ProductDetail,
  type ProductFormValues,
} from "@/lib/api/products";
import { ApiError } from "@/lib/api/client";

interface ProductFormDialogProps {
  visible: boolean;
  onHide: () => void;
  onSaved: (product: ProductDetail) => void;
  /** Present when editing an existing product; absent when adding a new one (FR-009/FR-010). */
  product?: ProductDetail;
}

const emptyForm: ProductFormValues = {
  name: "",
  price: 0,
  quantityOnHand: 0,
  barcode: "",
  lowStockThreshold: "",
  image: null,
};

function toFormValues(product?: ProductDetail): ProductFormValues {
  if (!product) return emptyForm;
  return {
    name: product.name,
    price: product.price,
    quantityOnHand: product.quantityOnHand,
    barcode: product.barcode ?? "",
    lowStockThreshold: product.lowStockThreshold?.toString() ?? "",
    image: null,
  };
}

// FR-009 (add) / FR-010 (edit): name, image upload, price, quantity, barcode, low-stock threshold.
export function ProductFormDialog({ visible, onHide, onSaved, product }: ProductFormDialogProps) {
  const { token } = useAuth();
  const [values, setValues] = useState<ProductFormValues>(() => toFormValues(product));
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setValues(toFormValues(product));
    setError(null);
  }, [product, visible]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!token) return;
    setIsSubmitting(true);
    setError(null);
    try {
      const saved = product
        ? await updateProduct(token, product.id, values)
        : await createProduct(token, values);
      onSaved(saved);
      onHide();
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "Unable to save this product. Please try again.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Dialog
      header={product ? "Edit product" : "Add product"}
      visible={visible}
      onHide={onHide}
      className="w-full max-w-md rounded-lg bg-white p-4 shadow-lg"
    >
      <form onSubmit={handleSubmit} className="space-y-3">
        <div className="space-y-1">
          <label className="text-sm font-medium text-gray-700">Name</label>
          <InputText
            value={values.name}
            onChange={(e) => setValues((v) => ({ ...v, name: e.target.value }))}
            className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
            placeholder="เช่น เงาะ"
            required
          />
          <p className="text-xs text-gray-500">
            ตั้งชื่อสินค้าให้ชัดเจน เช่น &quot;เงาะ&quot;, &quot;มะม่วง&quot; แทนคำกว้าง ๆ เช่น &quot;ผลไม้&quot;, &quot;สินค้า 1&quot;
          </p>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Price</label>
            <InputText
              type="number"
              min={0.01}
              step="0.01"
              value={String(values.price)}
              onChange={(e) => setValues((v) => ({ ...v, price: Number(e.target.value) }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              required
            />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Quantity on hand</label>
            <InputText
              type="number"
              min={0}
              value={String(values.quantityOnHand)}
              onChange={(e) => setValues((v) => ({ ...v, quantityOnHand: Number(e.target.value) }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              required
            />
          </div>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Barcode (optional)</label>
            <InputText
              value={values.barcode}
              onChange={(e) => setValues((v) => ({ ...v, barcode: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
            />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Low-stock threshold</label>
            <InputText
              type="number"
              min={0}
              value={values.lowStockThreshold}
              onChange={(e) => setValues((v) => ({ ...v, lowStockThreshold: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
            />
          </div>
        </div>

        <div className="space-y-1">
          <label className="text-sm font-medium text-gray-700">
            Image {product && "(leave empty to keep current)"}
          </label>
          <input
            type="file"
            accept="image/*"
            onChange={(e) => setValues((v) => ({ ...v, image: e.target.files?.[0] ?? null }))}
            className="block w-full text-sm text-gray-600"
          />
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
