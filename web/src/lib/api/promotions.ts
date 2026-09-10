import { apiFetch } from "@/lib/api/client";

export type PromotionScope = "PerProduct" | "WholeBill" | "MemberDiscount";

export interface Promotion {
  id: string;
  scope: PromotionScope;
  productId: string | null;
  discountPercent: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

export interface PromotionListResult {
  items: Promotion[];
}

export interface PromotionFormValues {
  scope: PromotionScope;
  /** Only sent when scope is "PerProduct"; empty string otherwise. */
  productId: string;
  discountPercent: number;
  startDate: string;
  endDate: string;
}

function toBody(values: PromotionFormValues): string {
  return JSON.stringify({
    scope: values.scope,
    productId: values.scope === "PerProduct" ? values.productId : null,
    discountPercent: values.discountPercent,
    startDate: values.startDate,
    endDate: values.endDate,
  });
}

// contracts/promotions.md GET /promotions
export function listPromotions(token: string, activeOnly = false): Promise<PromotionListResult> {
  const params = new URLSearchParams();
  if (activeOnly) {
    params.set("activeOnly", "true");
  }
  return apiFetch<PromotionListResult>(`/promotions?${params.toString()}`, { token });
}

// contracts/promotions.md POST /promotions (FR-020–FR-022)
export function createPromotion(token: string, values: PromotionFormValues): Promise<Promotion> {
  return apiFetch<Promotion>("/promotions", { method: "POST", token, body: toBody(values) });
}

// contracts/promotions.md PUT /promotions/{id}
export function updatePromotion(token: string, id: string, values: PromotionFormValues): Promise<Promotion> {
  return apiFetch<Promotion>(`/promotions/${id}`, { method: "PUT", token, body: toBody(values) });
}

// contracts/promotions.md DELETE /promotions/{id} — deactivates, doesn't delete the record.
export function deactivatePromotion(token: string, id: string): Promise<void> {
  return apiFetch<void>(`/promotions/${id}`, { method: "DELETE", token });
}
