import type { StaffRole } from "@/lib/api/auth";

export interface MenuItem {
  label: string;
  href: string;
  icon: string;
  allowedRoles: StaffRole[];
}

// FR-002/FR-003: fixed set of existing screens plus the new "จัดการสิทธิ์" screen (User Story 2).
// allowedRoles mirrors each screen's actual guard in web/src/app/(app)/*/layout.tsx — verified by
// reading the code, not assumed: only "sales" has no extra guard (any authenticated staff); every
// other existing screen already redirects non-Admins to /sales.
export const MENU_ITEMS: MenuItem[] = [
  { label: "ขายสินค้า", href: "/sales", icon: "pi pi-shopping-cart", allowedRoles: ["Cashier", "Admin"] },
  { label: "สต็อกสินค้า", href: "/stock", icon: "pi pi-box", allowedRoles: ["Admin"] },
  { label: "โปรโมชั่น", href: "/promotions", icon: "pi pi-percentage", allowedRoles: ["Admin"] },
  { label: "ประวัติการขาย", href: "/sales-history", icon: "pi pi-history", allowedRoles: ["Admin"] },
  { label: "รายงาน", href: "/reports", icon: "pi pi-chart-bar", allowedRoles: ["Admin"] },
  { label: "จัดการสิทธิ์", href: "/staff", icon: "pi pi-users", allowedRoles: ["Admin"] },
];
