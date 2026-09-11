"use client";

import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "primereact/dialog";
import { InputText } from "primereact/inputtext";
import { Button } from "primereact/button";
import { useAuth } from "@/lib/auth/AuthProvider";
import type { StaffRole } from "@/lib/api/auth";
import { createStaff, updateStaff, type StaffSummary } from "@/lib/api/staff";
import { ApiError } from "@/lib/api/client";

interface StaffFormDialogProps {
  visible: boolean;
  onHide: () => void;
  onSaved: (staff: StaffSummary) => void;
  /** Present when editing an existing staff account; absent when adding a new one (FR-008/FR-010). */
  staff?: StaffSummary;
}

interface FormValues {
  name: string;
  username: string;
  password: string;
  role: StaffRole;
}

function toFormValues(staff?: StaffSummary): FormValues {
  if (!staff) return { name: "", username: "", password: "", role: "Cashier" };
  return { name: staff.name, username: staff.username, password: "", role: staff.role };
}

// FR-008 (add: name/username/password/role) / FR-010 (edit: name/role only — username is
// immutable and password changes go through the separate "Reset password" action, clarification Q1).
export function StaffFormDialog({ visible, onHide, onSaved, staff }: StaffFormDialogProps) {
  const { token } = useAuth();
  const [values, setValues] = useState<FormValues>(() => toFormValues(staff));
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setValues(toFormValues(staff));
    setError(null);
  }, [staff, visible]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!token) return;
    setIsSubmitting(true);
    setError(null);
    try {
      const saved = staff
        ? await updateStaff(token, staff.id, { name: values.name, role: values.role })
        : await createStaff(token, values);
      onSaved(saved);
      onHide();
    } catch (err) {
      // FR-009: surface duplicate-username and other 422 validation messages from the API as-is.
      setError(
        err instanceof ApiError ? err.message : "Unable to save this staff account. Please try again.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Dialog
      header={staff ? "Edit staff" : "Add staff"}
      visible={visible}
      onHide={onHide}
      className="w-full max-w-md rounded-lg bg-white p-4 shadow-lg"
    >
      <form onSubmit={handleSubmit} className="space-y-3">
        <div className="space-y-1">
          <label className="text-sm font-medium text-gray-700">Name</label>
          <InputText
            value={values.name}
            onChange={(e) => setValues((v) => ({ ...v, name: e.target.value }))}
            className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
            required
          />
        </div>

        <div className="space-y-1">
          <label className="text-sm font-medium text-gray-700">Username</label>
          <InputText
            value={values.username}
            onChange={(e) => setValues((v) => ({ ...v, username: e.target.value }))}
            className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm disabled:bg-gray-100"
            disabled={!!staff}
            required
          />
        </div>

        {!staff && (
          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Password</label>
            <InputText
              type="password"
              value={values.password}
              onChange={(e) => setValues((v) => ({ ...v, password: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              minLength={8}
              required
            />
          </div>
        )}

        <div className="space-y-1">
          <label className="text-sm font-medium text-gray-700">Role</label>
          <select
            value={values.role}
            onChange={(e) => setValues((v) => ({ ...v, role: e.target.value as StaffRole }))}
            className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
          >
            <option value="Cashier">Cashier</option>
            <option value="Admin">Admin</option>
          </select>
        </div>

        {error && <p className="text-sm text-red-600">{error}</p>}

        <div className="flex justify-end gap-2 pt-2">
          <Button
            type="button"
            label="Cancel"
            onClick={onHide}
            className="rounded-md px-3 py-1.5 text-sm text-gray-600"
          />
          <Button
            type="submit"
            label={isSubmitting ? "Saving..." : "Save"}
            disabled={isSubmitting}
            className="rounded-md bg-gray-900 px-4 py-1.5 text-sm font-medium text-white disabled:opacity-50"
          />
        </div>
      </form>
    </Dialog>
  );
}
