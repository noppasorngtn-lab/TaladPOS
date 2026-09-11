"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth/AuthProvider";

// Permission management is Admin-only (FR-006); Cashiers are redirected to the sales screen.
export default function StaffLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const { staff, isLoading } = useAuth();

  useEffect(() => {
    if (!isLoading && staff && staff.role !== "Admin") {
      router.replace("/sales");
    }
  }, [isLoading, staff, router]);

  if (isLoading || staff?.role !== "Admin") {
    return null;
  }

  return <>{children}</>;
}
