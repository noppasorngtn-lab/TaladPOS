"use client";

import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { login as loginRequest, type StaffSummary } from "@/lib/api/auth";

interface AuthState {
  token: string | null;
  staff: StaffSummary | null;
}

interface AuthContextValue extends AuthState {
  isLoading: boolean;
  login: (username: string, password: string) => Promise<StaffSummary>;
  logout: () => void;
}

const STORAGE_KEY = "taladpos.auth";

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readStoredAuth(): AuthState {
  if (typeof window === "undefined") {
    return { token: null, staff: null };
  }
  const raw = window.localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return { token: null, staff: null };
  }
  try {
    return JSON.parse(raw) as AuthState;
  } catch {
    return { token: null, staff: null };
  }
}

/** Session state shared across the app (FR-007 — every screen requires a logged-in staff member). */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({ token: null, staff: null });
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    setState(readStoredAuth());
    setIsLoading(false);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      ...state,
      isLoading,
      login: async (username: string, password: string) => {
        const response = await loginRequest(username, password);
        const next: AuthState = { token: response.token, staff: response.staff };
        setState(next);
        window.localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
        return response.staff;
      },
      logout: () => {
        setState({ token: null, staff: null });
        window.localStorage.removeItem(STORAGE_KEY);
      },
    }),
    [state, isLoading],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
