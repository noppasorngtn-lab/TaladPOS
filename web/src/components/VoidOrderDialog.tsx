"use client";

import { useState } from "react";
import { Dialog } from "primereact/dialog";
import { Button } from "primereact/button";
import { useAuth } from "@/lib/auth/AuthProvider";
import { voidSalesOrder, type SalesOrder } from "@/lib/api/salesOrders";
import { ApiError } from "@/lib/api/client";

interface VoidOrderDialogProps {
  order: SalesOrder;
  onVoided: (order: SalesOrder) => void;
}

// Admin-only void action (FR-027/FR-028) — the only place in User Story 1 a completed
// order can be voided, since sale history (User Story 5) doesn't exist yet.
export function VoidOrderDialog({ order, onVoided }: VoidOrderDialogProps) {
  const { token, staff } = useAuth();
  const [isOpen, setIsOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (staff?.role !== "Admin" || order.status === "Voided" || !token) {
    return null;
  }

  async function handleConfirm() {
    setIsSubmitting(true);
    setError(null);
    try {
      const voided = await voidSalesOrder(token!, order.id);
      onVoided(voided);
      setIsOpen(false);
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "Unable to void this order. Please try again.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <>
      <Button
        type="button"
        label="Void"
        onClick={() => setIsOpen(true)}
        className="rounded-md border border-red-300 px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50"
      />
      <Dialog
        header="Void this order?"
        visible={isOpen}
        onHide={() => setIsOpen(false)}
        className="w-full max-w-sm rounded-lg bg-white p-4 shadow-lg"
      >
        <p className="text-sm text-gray-600">
          This restores stock for every line and removes the order from sales reports. This can only
          be done on the same day the order was created and cannot be undone.
        </p>
        {error && <p className="mt-2 text-sm text-red-600">{error}</p>}
        <div className="mt-4 flex justify-end gap-2">
          <Button
            type="button"
            label="Cancel"
            onClick={() => setIsOpen(false)}
            className="rounded-md px-3 py-1.5 text-sm text-gray-600"
          />
          <Button
            type="button"
            label={isSubmitting ? "Voiding..." : "Void order"}
            onClick={handleConfirm}
            disabled={isSubmitting}
            className="rounded-md bg-red-600 px-3 py-1.5 text-sm font-medium text-white disabled:opacity-50"
          />
        </div>
      </Dialog>
    </>
  );
}
