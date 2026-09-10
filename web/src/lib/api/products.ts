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

export interface ProductDetail {
  id: string;
  name: string;
  imageUrl: string | null;
  price: number;
  quantityOnHand: number;
  barcode: string | null;
  lowStockThreshold: number | null;
  isActive: boolean;
  lowStock: boolean;
}

export interface ProductFormValues {
  name: string;
  price: number;
  quantityOnHand: number;
  barcode: string;
  lowStockThreshold: string;
  image: File | null;
}

// contracts/products.md GET /products (search-only usage in User Story 1;
// includeInactive/low-stock are Admin-only, added for User Story 2)
export function searchProducts(
  token: string,
  search: string,
  includeInactive = false,
): Promise<ProductSearchResult> {
  const params = new URLSearchParams();
  if (search) {
    params.set("search", search);
  }
  if (includeInactive) {
    params.set("includeInactive", "true");
  }
  return apiFetch<ProductSearchResult>(`/products?${params.toString()}`, { token });
}

// contracts/products.md GET /products/low-stock (FR-013)
export function getLowStockProducts(token: string): Promise<ProductSearchResult> {
  return apiFetch<ProductSearchResult>("/products/low-stock", { token });
}

// contracts/products.md GET /products/{id} — full detail, used to prefill the edit form
export function getProduct(token: string, id: string): Promise<ProductDetail> {
  return apiFetch<ProductDetail>(`/products/${id}`, { token });
}

function toFormData(values: ProductFormValues): FormData {
  const formData = new FormData();
  formData.set("name", values.name);
  formData.set("price", String(values.price));
  formData.set("quantityOnHand", String(values.quantityOnHand));
  if (values.barcode) {
    formData.set("barcode", values.barcode);
  }
  if (values.lowStockThreshold) {
    formData.set("lowStockThreshold", values.lowStockThreshold);
  }
  if (values.image) {
    formData.set("image", values.image);
  }
  return formData;
}

// contracts/products.md POST /products (FR-009)
export function createProduct(token: string, values: ProductFormValues): Promise<ProductDetail> {
  return apiFetch<ProductDetail>("/products", { method: "POST", token, body: toFormData(values) });
}

// contracts/products.md PUT /products/{id} (FR-010)
export function updateProduct(
  token: string,
  id: string,
  values: ProductFormValues,
): Promise<ProductDetail> {
  return apiFetch<ProductDetail>(`/products/${id}`, {
    method: "PUT",
    token,
    body: toFormData(values),
  });
}

// contracts/products.md DELETE /products/{id} — soft delete (FR-011)
export function deleteProduct(token: string, id: string): Promise<void> {
  return apiFetch<void>(`/products/${id}`, { method: "DELETE", token });
}
