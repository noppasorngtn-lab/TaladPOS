"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth/AuthProvider";
import { NavMenu } from "@/components/NavMenu";

/**
 * Shared guard for every authenticated screen (sales, stock, members, promotions,
 * sales-history, reports, staff) — FR-007: every screen requires a logged-in staff member.
 * Also renders the central nav menu (FR-001) on every screen this layout wraps.
 */
export default function AuthenticatedLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const { token, isLoading } = useAuth();

  useEffect(() => {
    if (!isLoading && !token) {
      router.replace("/login");
    }
  }, [isLoading, token, router]);

  if (isLoading || !token) {
    return null;
  }

  return (
    <>
      <NavMenu />
      {children}
    </>
  );
}
