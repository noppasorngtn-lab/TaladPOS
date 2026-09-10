"use client";

import { PrimeReactProvider } from "primereact/api";
import type { ReactNode } from "react";

/**
 * Mounts PrimeReact in unstyled mode so Tailwind (+ tailwindcss-primeui) is the
 * only visual styling system, per Constitution Principle IV and research.md item 9.
 */
export function PrimeReactAppProvider({ children }: { children: ReactNode }) {
  return <PrimeReactProvider value={{ unstyled: true }}>{children}</PrimeReactProvider>;
}
