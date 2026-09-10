import type { Config } from "tailwindcss";
import primeui from "tailwindcss-primeui";

const config: Config = {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
    "./node_modules/primereact/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        background: "var(--background)",
        foreground: "var(--foreground)",
      },
    },
  },
  // tailwindcss-primeui lets Tailwind utility classes style PrimeReact's
  // unstyled components (research.md item 9) so Tailwind stays the only
  // visual styling system per Constitution Principle IV.
  plugins: [primeui],
};
export default config;
