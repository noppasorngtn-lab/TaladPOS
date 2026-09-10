// FR-007: the sales screen requires an authenticated session but allows any role
// (Cashier or Admin) — the authentication check itself is already enforced by the
// parent (app)/layout.tsx guard, so this layout has nothing extra to add.
export default function SalesLayout({ children }: { children: React.ReactNode }) {
  return <>{children}</>;
}
