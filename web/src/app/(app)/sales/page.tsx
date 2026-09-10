"use client";

import { useEffect, useState } from "react";
import { InputText } from "primereact/inputtext";
import { Button } from "primereact/button";
import { useAuth } from "@/lib/auth/AuthProvider";
import { searchProducts, type ProductSummary } from "@/lib/api/products";
import {
  checkout,
  pricingPreview,
  type PricingPreview,
  type SalesOrder,
} from "@/lib/api/salesOrders";
import { ApiError } from "@/lib/api/client";
import { findMemberByPhone, type Member } from "@/lib/api/members";
import { ProductCard } from "@/components/ProductCard";
import { Cart, type CartLine } from "@/components/Cart";
import { VoidOrderDialog } from "@/components/VoidOrderDialog";
import { MemberSignupDialog } from "@/components/MemberSignupDialog";

export default function SalesPage() {
  const { token } = useAuth();
  const [search, setSearch] = useState("");
  const [products, setProducts] = useState<ProductSummary[]>([]);
  const [cartLines, setCartLines] = useState<CartLine[]>([]);
  const [pricing, setPricing] = useState<PricingPreview | null>(null);
  const [isCheckingOut, setIsCheckingOut] = useState(false);
  const [checkoutError, setCheckoutError] = useState<string | null>(null);
  const [completedOrder, setCompletedOrder] = useState<SalesOrder | null>(null);

  // FR-017: link a sale to an existing member by phone number (US3).
  const [memberPhoneInput, setMemberPhoneInput] = useState("");
  const [linkedMember, setLinkedMember] = useState<Member | null>(null);
  const [memberNotFoundPhone, setMemberNotFoundPhone] = useState<string | null>(null);
  const [memberLookupError, setMemberLookupError] = useState<string | null>(null);
  const [isSignupDialogVisible, setIsSignupDialogVisible] = useState(false);

  // FR-002: search-as-you-type by name or barcode.
  useEffect(() => {
    if (!token) return;
    let cancelled = false;
    searchProducts(token, search)
      .then((result) => {
        if (!cancelled) setProducts(result.items);
      })
      .catch(() => {
        if (!cancelled) setProducts([]);
      });
    return () => {
      cancelled = true;
    };
  }, [token, search]);

  // FR-004: recompute the live total from the server whenever the cart changes
  // (research.md item 5 — the frontend never re-implements pricing math). Member discounts
  // aren't wired server-side until User Story 4, so linking a member won't move this total yet.
  useEffect(() => {
    if (!token || cartLines.length === 0) {
      setPricing(null);
      return;
    }
    let cancelled = false;
    pricingPreview(
      token,
      cartLines.map((l) => ({ productId: l.productId, quantity: l.quantity })),
      linkedMember?.id,
    )
      .then((result) => {
        if (!cancelled) setPricing(result);
      })
      .catch(() => {
        if (!cancelled) setPricing(null);
      });
    return () => {
      cancelled = true;
    };
  }, [token, cartLines, linkedMember]);

  async function searchMember() {
    if (!token || !memberPhoneInput.trim()) return;
    setMemberLookupError(null);
    setMemberNotFoundPhone(null);
    try {
      const member = await findMemberByPhone(token, memberPhoneInput.trim());
      setLinkedMember(member);
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        // FR-017 edge case: no member with that phone — offer sign up or continue without one.
        setMemberNotFoundPhone(memberPhoneInput.trim());
      } else {
        setMemberLookupError(
          err instanceof ApiError ? err.message : "Could not look up this member. Please try again.",
        );
      }
    }
  }

  function unlinkMember() {
    setLinkedMember(null);
    setMemberPhoneInput("");
    setMemberNotFoundPhone(null);
    setMemberLookupError(null);
  }

  function addToCart(product: ProductSummary) {
    setCompletedOrder(null);
    setCartLines((prev) => {
      const existing = prev.find((l) => l.productId === product.id);
      if (existing) {
        if (existing.quantity >= product.quantityOnHand) return prev;
        return prev.map((l) =>
          l.productId === product.id ? { ...l, quantity: l.quantity + 1 } : l,
        );
      }
      return [
        ...prev,
        {
          productId: product.id,
          name: product.name,
          unitPrice: product.price,
          quantity: 1,
          quantityOnHand: product.quantityOnHand,
        },
      ];
    });
  }

  function increase(productId: string) {
    setCartLines((prev) =>
      prev.map((l) =>
        l.productId === productId && l.quantity < l.quantityOnHand
          ? { ...l, quantity: l.quantity + 1 }
          : l,
      ),
    );
  }

  function decrease(productId: string) {
    setCartLines((prev) =>
      prev
        .map((l) => (l.productId === productId ? { ...l, quantity: l.quantity - 1 } : l))
        .filter((l) => l.quantity > 0),
    );
  }

  function remove(productId: string) {
    setCartLines((prev) => prev.filter((l) => l.productId !== productId));
  }

  async function handleCheckout() {
    if (!token || cartLines.length === 0) return;
    setIsCheckingOut(true);
    setCheckoutError(null);
    try {
      const order = await checkout(
        token,
        cartLines.map((l) => ({ productId: l.productId, quantity: l.quantity })),
        linkedMember?.id,
      );
      setCompletedOrder(order);
      setCartLines([]);
      setPricing(null);
      unlinkMember();
      // Stock just changed — refresh the visible product list (FR-005/FR-011).
      const refreshed = await searchProducts(token, search);
      setProducts(refreshed.items);
    } catch (err) {
      // FR-006/SC-007: any failure (network or 409 insufficient stock) aborts the whole
      // transaction — the cart is left untouched so the cashier can adjust and retry.
      setCheckoutError(
        err instanceof ApiError
          ? err.message
          : "Could not reach the server. Please check your connection and try again.",
      );
    } finally {
      setIsCheckingOut(false);
    }
  }

  return (
    <main className="grid h-full grid-cols-[1fr_320px] gap-4 p-4">
      <section className="flex flex-col gap-4">
        <InputText
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search by name or barcode..."
          className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
        />

        {completedOrder && (
          <div className="flex items-center justify-between rounded-md border border-green-200 bg-green-50 px-4 py-3 text-sm">
            <span className="text-green-800">
              Sale completed — total ฿{completedOrder.netTotal.toFixed(2)}
            </span>
            <VoidOrderDialog order={completedOrder} onVoided={setCompletedOrder} />
          </div>
        )}

        {checkoutError && (
          <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {checkoutError}
          </div>
        )}

        <div className="grid grid-cols-2 gap-3 overflow-y-auto sm:grid-cols-3 md:grid-cols-4">
          {products.map((product) => (
            <ProductCard key={product.id} product={product} onSelect={addToCart} />
          ))}
          {products.length === 0 && (
            <p className="col-span-full text-sm text-gray-400">No products found.</p>
          )}
        </div>
      </section>

      <div className="flex h-full flex-col gap-3">
        <div className="rounded-lg border border-gray-200 bg-white p-3 text-sm">
          <h2 className="mb-2 font-semibold text-gray-900">Member</h2>

          {linkedMember ? (
            <div className="flex items-center justify-between">
              <div>
                <p className="font-medium text-gray-900">{linkedMember.name}</p>
                <p className="text-gray-500">{linkedMember.phoneNumber}</p>
              </div>
              <Button
                type="button"
                label="Unlink"
                onClick={unlinkMember}
                className="rounded-md px-2 py-1 text-xs text-gray-600 hover:bg-gray-100"
              />
            </div>
          ) : (
            <div className="flex gap-2">
              <InputText
                value={memberPhoneInput}
                onChange={(e) => {
                  setMemberPhoneInput(e.target.value);
                  setMemberNotFoundPhone(null);
                  setMemberLookupError(null);
                }}
                placeholder="Phone number"
                className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
              />
              <Button
                type="button"
                label="Find"
                onClick={searchMember}
                className="rounded-md border border-gray-300 px-3 py-1.5 text-sm text-gray-700"
              />
            </div>
          )}

          {memberNotFoundPhone && (
            <div className="mt-2 flex items-center justify-between gap-2 text-xs text-gray-600">
              <span>No member with this phone number.</span>
              <div className="flex gap-2">
                <Button
                  type="button"
                  label="Sign up"
                  onClick={() => setIsSignupDialogVisible(true)}
                  className="rounded-md bg-gray-900 px-2 py-1 text-white"
                />
                <Button
                  type="button"
                  label="Continue"
                  onClick={() => setMemberNotFoundPhone(null)}
                  className="rounded-md border border-gray-300 px-2 py-1 text-gray-700"
                />
              </div>
            </div>
          )}

          {memberLookupError && <p className="mt-2 text-xs text-red-600">{memberLookupError}</p>}
        </div>

        <Cart
          lines={cartLines}
          pricing={pricing}
          onIncrease={increase}
          onDecrease={decrease}
          onRemove={remove}
          onCheckout={handleCheckout}
          isCheckingOut={isCheckingOut}
        />
      </div>

      <MemberSignupDialog
        visible={isSignupDialogVisible}
        onHide={() => setIsSignupDialogVisible(false)}
        onSignedUp={(member) => {
          setLinkedMember(member);
          setMemberNotFoundPhone(null);
        }}
        initialPhoneNumber={memberNotFoundPhone ?? undefined}
      />
    </main>
  );
}
