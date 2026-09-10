import { apiFetch } from "@/lib/api/client";

export interface SalesOrderLineRequest {
  productId: string;
  quantity: number;
}

export interface PricingPreview {
  subtotalAmount: number;
  promotionDiscountAmount: number;
  memberDiscountAmount: number;
  netTotal: number;
}

export interface SalesOrderLineResponse {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  lineDiscountAmount: number;
  lineTotal: number;
}

export interface SalesOrder {
  id: string;
  createdAt: string;
  staffId: string;
  memberId: string | null;
  status: "Completed" | "Voided";
  subtotalAmount: number;
  promotionDiscountAmount: number;
  memberDiscountAmount: number;
  netTotal: number;
  voidedAt: string | null;
  lines: SalesOrderLineResponse[];
}

function lineQueryParams(
  lines: SalesOrderLineRequest[],
  memberId?: string | null,
): URLSearchParams {
  const params = new URLSearchParams();
  if (memberId) {
    params.set("memberId", memberId);
  }
  for (const line of lines) {
    params.append("productId", line.productId);
    params.append("quantity", String(line.quantity));
  }
  return params;
}

// contracts/sales-orders.md GET /sales-orders/pricing-preview — the source of truth for cart
// totals (research.md item 5); the frontend never re-implements discount math itself.
export function pricingPreview(
  token: string,
  lines: SalesOrderLineRequest[],
  memberId?: string | null,
): Promise<PricingPreview> {
  return apiFetch<PricingPreview>(
    `/sales-orders/pricing-preview?${lineQueryParams(lines, memberId).toString()}`,
    {
      token,
    },
  );
}

// contracts/sales-orders.md POST /sales-orders
export function checkout(
  token: string,
  lines: SalesOrderLineRequest[],
  memberId?: string | null,
): Promise<SalesOrder> {
  return apiFetch<SalesOrder>("/sales-orders", {
    method: "POST",
    token,
    body: JSON.stringify({ memberId: memberId ?? null, lines }),
  });
}

// contracts/sales-orders.md POST /sales-orders/{id}/void
export function voidSalesOrder(token: string, id: string): Promise<SalesOrder> {
  return apiFetch<SalesOrder>(`/sales-orders/${id}/void`, { method: "POST", token });
}
