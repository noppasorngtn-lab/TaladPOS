import { apiFetch } from "@/lib/api/client";

export type StaffRole = "Cashier" | "Admin";

export interface StaffSummary {
  id: string;
  name: string;
  role: StaffRole;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  staff: StaffSummary;
}

// contracts/auth.md POST /auth/login
export function login(username: string, password: string): Promise<LoginResponse> {
  return apiFetch<LoginResponse>("/auth/login", {
    method: "POST",
    body: JSON.stringify({ username, password }),
  });
}
