import { Button } from "primereact/button";
import type { PricingPreview } from "@/lib/api/salesOrders";

export interface CartLine {
  productId: string;
  name: string;
  unitPrice: number;
  quantity: number;
  quantityOnHand: number;
}

interface CartProps {
  lines: CartLine[];
  pricing: PricingPreview | null;
  onIncrease: (productId: string) => void;
  onDecrease: (productId: string) => void;
  onRemove: (productId: string) => void;
  onCheckout: () => void;
  isCheckingOut: boolean;
}

function money(amount: number): string {
  return `฿${amount.toFixed(2)}`;
}

// FR-003/FR-004: add/adjust/remove lines before checkout, with the total recalculated live.
export function Cart({
  lines,
  pricing,
  onIncrease,
  onDecrease,
  onRemove,
  onCheckout,
  isCheckingOut,
}: CartProps) {
  const fallbackTotal = lines.reduce((sum, line) => sum + line.unitPrice * line.quantity, 0);
  const total = pricing?.netTotal ?? fallbackTotal;

  return (
    <aside className="flex h-full flex-col rounded-lg border border-gray-200 bg-white p-4">
      <h2 className="mb-3 text-sm font-semibold text-gray-900">Cart</h2>

      {lines.length === 0 ? (
        <p className="flex-1 text-sm text-gray-400">No items yet — tap a product to add it.</p>
      ) : (
        <ul className="flex-1 space-y-3 overflow-y-auto">
          {lines.map((line) => (
            <li key={line.productId} className="flex items-center justify-between gap-2 text-sm">
              <div className="min-w-0 flex-1">
                <p className="truncate font-medium text-gray-900">{line.name}</p>
                <p className="text-gray-500">{money(line.unitPrice)} each</p>
              </div>
              <div className="flex items-center gap-1">
                <Button
                  type="button"
                  icon="pi pi-minus"
                  onClick={() => onDecrease(line.productId)}
                  className="flex h-7 w-7 items-center justify-center rounded border border-gray-300 text-gray-600"
                />
                <span className="w-6 text-center">{line.quantity}</span>
                <Button
                  type="button"
                  icon="pi pi-plus"
                  onClick={() => onIncrease(line.productId)}
                  disabled={line.quantity >= line.quantityOnHand}
                  className="flex h-7 w-7 items-center justify-center rounded border border-gray-300 text-gray-600 disabled:opacity-40"
                />
                <Button
                  type="button"
                  icon="pi pi-trash"
                  onClick={() => onRemove(line.productId)}
                  className="ml-1 flex h-7 w-7 items-center justify-center rounded text-red-500 hover:bg-red-50"
                />
              </div>
            </li>
          ))}
        </ul>
      )}

      <div className="mt-4 border-t border-gray-200 pt-3">
        {pricing && (
          <div className="mb-2 space-y-1 text-sm text-gray-600">
            <div className="flex items-center justify-between">
              <span>Subtotal</span>
              <span>{money(pricing.subtotalAmount)}</span>
            </div>
            {/* FR-024: each discount type shown separately, never combined into one line. */}
            {pricing.promotionDiscountAmount > 0 && (
              <div className="flex items-center justify-between text-amber-700">
                <span>Promotion discount</span>
                <span>-{money(pricing.promotionDiscountAmount)}</span>
              </div>
            )}
            {pricing.memberDiscountAmount > 0 && (
              <div className="flex items-center justify-between text-amber-700">
                <span>Member discount</span>
                <span>-{money(pricing.memberDiscountAmount)}</span>
              </div>
            )}
          </div>
        )}
        <div className="flex items-center justify-between text-sm font-semibold text-gray-900">
          <span>Total</span>
          <span>{money(total)}</span>
        </div>
        <Button
          type="button"
          label={isCheckingOut ? "Processing..." : "Checkout"}
          onClick={onCheckout}
          disabled={lines.length === 0 || isCheckingOut}
          className="mt-3 w-full rounded-md bg-gray-900 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
        />
      </div>
    </aside>
  );
}
