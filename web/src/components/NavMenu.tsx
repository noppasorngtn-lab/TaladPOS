"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/lib/auth/AuthProvider";
import { MENU_ITEMS } from "@/lib/navigation";

// FR-001/FR-003/FR-004/FR-005: role-filtered nav rendered on every authenticated screen, with the
// current screen highlighted and client-side navigation via next/link.
export function NavMenu() {
  const pathname = usePathname();
  const { staff, isLoading, logout } = useAuth();

  if (isLoading || !staff) {
    return null;
  }

  const items = MENU_ITEMS.filter((item) => item.allowedRoles.includes(staff.role));

  return (
    <nav className="flex items-center justify-between border-b border-gray-200 bg-white px-4 py-2">
      <ul className="flex flex-wrap items-center gap-1">
        {items.map((item) => {
          const isActive = pathname === item.href || pathname?.startsWith(`${item.href}/`);
          return (
            <li key={item.href}>
              <Link
                href={item.href}
                className={`flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium ${
                  isActive
                    ? "bg-gray-900 text-white"
                    : "text-gray-700 hover:bg-gray-100"
                }`}
              >
                <i className={item.icon} aria-hidden="true" />
                {item.label}
              </Link>
            </li>
          );
        })}
      </ul>
      <div className="flex items-center gap-3 text-sm text-gray-500">
        <span>
          {staff.name} ({staff.role})
        </span>
        <button
          type="button"
          onClick={logout}
          className="rounded-md border border-gray-300 px-2 py-1 text-xs text-gray-700 hover:bg-gray-100"
        >
          ออกจากระบบ
        </button>
      </div>
    </nav>
  );
}
