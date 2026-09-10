"use client";

import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "primereact/dialog";
import { InputText } from "primereact/inputtext";
import { Button } from "primereact/button";
import { useAuth } from "@/lib/auth/AuthProvider";
import { signUpMember, type Member } from "@/lib/api/members";
import { ApiError } from "@/lib/api/client";

interface MemberSignupDialogProps {
  visible: boolean;
  onHide: () => void;
  onSignedUp: (member: Member) => void;
  /** Prefills the phone field when opened from a failed "not found" lookup. */
  initialPhoneNumber?: string;
}

// FR-015: sign up a new member (name + phone number) directly from the Sales screen.
export function MemberSignupDialog({
  visible,
  onHide,
  onSignedUp,
  initialPhoneNumber = "",
}: MemberSignupDialogProps) {
  const { token } = useAuth();
  const [name, setName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState(initialPhoneNumber);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (visible) {
      setName("");
      setPhoneNumber(initialPhoneNumber);
      setError(null);
    }
  }, [visible, initialPhoneNumber]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!token) return;
    setIsSubmitting(true);
    setError(null);
    try {
      const member = await signUpMember(token, name, phoneNumber);
      onSignedUp(member);
      onHide();
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "Unable to sign up this member. Please try again.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Dialog
      header="Sign up member"
      visible={visible}
      onHide={onHide}
      className="w-full max-w-sm rounded-lg bg-white p-4 shadow-lg"
    >
      <form onSubmit={handleSubmit} className="space-y-3">
        <div className="space-y-1">
          <label className="text-sm font-medium text-gray-700">Name</label>
          <InputText
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
            required
          />
        </div>

        <div className="space-y-1">
          <label className="text-sm font-medium text-gray-700">Phone number</label>
          <InputText
            value={phoneNumber}
            onChange={(e) => setPhoneNumber(e.target.value)}
            className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
            required
          />
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
            label={isSubmitting ? "Signing up..." : "Sign up"}
            disabled={isSubmitting}
            className="rounded-md bg-gray-900 px-4 py-1.5 text-sm font-medium text-white disabled:opacity-50"
          />
        </div>
      </form>
    </Dialog>
  );
}
