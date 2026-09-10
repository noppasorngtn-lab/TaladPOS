"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth/AuthProvider";

// Reports are Admin-only (FR-029–FR-032); Cashiers are redirected to the sales screen.
export default function ReportsLayout({ children }: { children: React.ReactNode }) {
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
