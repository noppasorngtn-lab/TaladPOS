import { apiFetch } from "@/lib/api/client";

export interface ProductSummary {
  id: string;
  name: string;
  imageUrl: string | null;
  price: number;
  quantityOnHand: number;
  barcode: string | null;
  lowStock: boolean;
}

export interface ProductSearchResult {
  items: ProductSummary[];
  total: number;
}

// contracts/products.md GET /products (search-only usage in User Story 1)
export function searchProducts(token: string, search: string): Promise<ProductSearchResult> {
  const params = new URLSearchParams();
  if (search) {
    params.set("search", search);
  }
  return apiFetch<ProductSearchResult>(`/products?${params.toString()}`, { token });
}
