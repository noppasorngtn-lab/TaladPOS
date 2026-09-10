import { apiFetch } from "@/lib/api/client";

export interface Member {
  id: string;
  name: string;
  phoneNumber: string;
  accumulatedPurchaseTotal: number;
  joinedAt: string;
}

// contracts/members.md GET /members?phone= (FR-017) — 404s when no member has that phone number.
export function findMemberByPhone(token: string, phone: string): Promise<Member> {
  return apiFetch<Member>(`/members?phone=${encodeURIComponent(phone)}`, { token });
}

// contracts/members.md POST /members (FR-015)
export function signUpMember(token: string, name: string, phoneNumber: string): Promise<Member> {
  return apiFetch<Member>("/members", {
    method: "POST",
    token,
    body: JSON.stringify({ name, phoneNumber }),
  });
}
