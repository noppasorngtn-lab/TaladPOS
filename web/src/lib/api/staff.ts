import { apiFetch } from "@/lib/api/client";
import type { StaffRole } from "@/lib/api/auth";

export interface StaffSummary {
  id: string;
  name: string;
  username: string;
  role: StaffRole;
  isActive: boolean;
}

export interface StaffSearchResult {
  items: StaffSummary[];
  total: number;
}

export interface CreateStaffValues {
  name: string;
  username: string;
  password: string;
  role: StaffRole;
}

export interface EditStaffValues {
  name: string;
  role: StaffRole;
}

// contracts/staff.md GET /staff
export function listStaff(
  token: string,
  search: string,
  page = 1,
  pageSize = 50,
): Promise<StaffSearchResult> {
  const params = new URLSearchParams();
  if (search) {
    params.set("search", search);
  }
  params.set("page", String(page));
  params.set("pageSize", String(pageSize));
  return apiFetch<StaffSearchResult>(`/staff?${params.toString()}`, { token });
}

// contracts/staff.md GET /staff/{id}
export function getStaff(token: string, id: string): Promise<StaffSummary> {
  return apiFetch<StaffSummary>(`/staff/${id}`, { token });
}

// contracts/staff.md POST /staff (FR-008)
export function createStaff(token: string, values: CreateStaffValues): Promise<StaffSummary> {
  return apiFetch<StaffSummary>("/staff", {
    method: "POST",
    token,
    body: JSON.stringify(values),
  });
}

// contracts/staff.md PUT /staff/{id} (FR-010)
export function updateStaff(token: string, id: string, values: EditStaffValues): Promise<StaffSummary> {
  return apiFetch<StaffSummary>(`/staff/${id}`, {
    method: "PUT",
    token,
    body: JSON.stringify(values),
  });
}

// contracts/staff.md POST /staff/{id}/deactivate (FR-011)
export function deactivateStaff(token: string, id: string): Promise<void> {
  return apiFetch<void>(`/staff/${id}/deactivate`, { method: "POST", token });
}

// contracts/staff.md POST /staff/{id}/activate
export function activateStaff(token: string, id: string): Promise<void> {
  return apiFetch<void>(`/staff/${id}/activate`, { method: "POST", token });
}

// contracts/staff.md POST /staff/{id}/reset-password (FR-016)
export function resetStaffPassword(token: string, id: string, newPassword: string): Promise<void> {
  return apiFetch<void>(`/staff/${id}/reset-password`, {
    method: "POST",
    token,
    body: JSON.stringify({ newPassword }),
  });
}
