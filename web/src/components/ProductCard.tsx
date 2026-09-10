import type { ProductSummary } from "@/lib/api/products";

interface ProductCardProps {
  product: ProductSummary;
  onSelect: (product: ProductSummary) => void;
}

// FR-001: sellable items shown as image cards the cashier can tap to add to the cart.
export function ProductCard({ product, onSelect }: ProductCardProps) {
  const outOfStock = product.quantityOnHand <= 0;

  return (
    <button
      type="button"
      onClick={() => onSelect(product)}
      disabled={outOfStock}
      className="flex flex-col items-stretch overflow-hidden rounded-lg border border-gray-200 bg-white text-left shadow-sm transition hover:shadow-md disabled:cursor-not-allowed disabled:opacity-50"
    >
      <div className="flex h-24 items-center justify-center bg-gray-100">
        {product.imageUrl ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={product.imageUrl} alt={product.name} className="h-full w-full object-cover" />
        ) : (
          <span className="text-3xl">🛒</span>
        )}
      </div>
      <div className="space-y-1 p-3">
        <p className="truncate text-sm font-medium text-gray-900">{product.name}</p>
        <p className="text-sm text-gray-600">฿{product.price.toFixed(2)}</p>
        {outOfStock ? (
          <p className="text-xs font-medium text-red-600">Out of stock</p>
        ) : (
          <p className="text-xs text-gray-400">{product.quantityOnHand} on hand</p>
        )}
      </div>
    </button>
  );
}
