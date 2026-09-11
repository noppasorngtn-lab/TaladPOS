"use client";

import { useCallback, useEffect, useState } from "react";
import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { Button } from "primereact/button";
import { InputText } from "primereact/inputtext";
import { useAuth } from "@/lib/auth/AuthProvider";
import {
  listStaff,
  deactivateStaff,
  activateStaff,
  resetStaffPassword,
  type StaffSummary,
} from "@/lib/api/staff";
import { ApiError } from "@/lib/api/client";
import { StaffFormDialog } from "@/components/StaffFormDialog";

export default function StaffPage() {
  const { token } = useAuth();
  const [search, setSearch] = useState("");
  const [staffList, setStaffList] = useState<StaffSummary[]>([]);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingStaff, setEditingStaff] = useState<StaffSummary | undefined>(undefined);
  const [error, setError] = useState<string | null>(null);

  // FR-007: the staff list reads from the Admin-only /staff endpoint (contracts/staff.md).
  const refresh = useCallback(async () => {
    if (!token) return;
    try {
      const result = await listStaff(token, search);
      setStaffList(result.items);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Unable to load staff.");
    }
  }, [token, search]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  function openAddDialog() {
    setEditingStaff(undefined);
    setIsDialogOpen(true);
  }

  function openEditDialog(staff: StaffSummary) {
    setEditingStaff(staff);
    setIsDialogOpen(true);
  }

  // FR-011/FR-012: toggling IsActive both directions; a 409 here means this account is the last
  // active Admin — surface the API's message as-is rather than re-implementing that check client-side.
  async function handleToggleActive(staff: StaffSummary) {
    if (!token) return;
    setError(null);
    try {
      if (staff.isActive) {
        await deactivateStaff(token, staff.id);
      } else {
        await activateStaff(token, staff.id);
      }
      await refresh();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Unable to update this staff account.");
    }
  }

  // FR-016: Admin sets a new password directly; no forced-change-on-next-login flow (clarification Q1).
  async function handleResetPassword(staff: StaffSummary) {
    if (!token) return;
    const newPassword = window.prompt(`New password for "${staff.username}" (at least 8 characters):`);
    if (!newPassword) return;
    setError(null);
    try {
      await resetStaffPassword(token, staff.id, newPassword);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Unable to reset this password.");
    }
  }

  return (
    <main className="flex flex-col gap-4 p-4">
      <div className="flex items-center justify-between">
        <h1 className="text-lg font-semibold text-gray-900">จัดการสิทธิ์</h1>
        <Button
          type="button"
          label="Add staff"
          onClick={openAddDialog}
          className="rounded-md bg-gray-900 px-4 py-2 text-sm font-medium text-white"
        />
      </div>

      {error && (
        <div className="rounded-md border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">
          {error}
        </div>
      )}

      <InputText
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder="Search by name or username..."
        className="w-full max-w-sm rounded-md border border-gray-300 px-3 py-2 text-sm"
      />

      <DataTable
        value={staffList}
        dataKey="id"
        className="w-full overflow-hidden rounded-lg border border-gray-200 bg-white text-sm"
      >
        <Column
          field="name"
          header="Name"
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
        />
        <Column
          field="username"
          header="Username"
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
        />
        <Column
          field="role"
          header="Role"
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
        />
        <Column
          header="Status"
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
          body={(row: StaffSummary) =>
            row.isActive ? (
              <span className="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800">
                Active
              </span>
            ) : (
              <span className="rounded-full bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-600">
                Inactive
              </span>
            )
          }
        />
        <Column
          header="Actions"
          headerClassName="border-b border-gray-200 px-4 py-2 text-left font-semibold text-gray-700"
          bodyClassName="border-b border-gray-100 px-4 py-2"
          body={(row: StaffSummary) => (
            <div className="flex gap-2">
              <Button
                type="button"
                label="Edit"
                onClick={() => openEditDialog(row)}
                className="rounded-md border border-gray-300 px-2 py-1 text-xs text-gray-700"
              />
              <Button
                type="button"
                label={row.isActive ? "Deactivate" : "Activate"}
                onClick={() => handleToggleActive(row)}
                className={`rounded-md border px-2 py-1 text-xs ${
                  row.isActive ? "border-red-300 text-red-600" : "border-green-300 text-green-700"
                }`}
              />
              <Button
                type="button"
                label="Reset password"
                onClick={() => handleResetPassword(row)}
                className="rounded-md border border-gray-300 px-2 py-1 text-xs text-gray-700"
              />
            </div>
          )}
        />
      </DataTable>

      <StaffFormDialog
        visible={isDialogOpen}
        onHide={() => setIsDialogOpen(false)}
        onSaved={() => refresh()}
        staff={editingStaff}
      />
    </main>
  );
}
