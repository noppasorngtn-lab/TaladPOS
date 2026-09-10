import type { Metadata } from "next";
import localFont from "next/font/local";
import "primeicons/primeicons.css";
import "./globals.css";
import { PrimeReactAppProvider } from "@/lib/providers/PrimeReactAppProvider";
import { AuthProvider } from "@/lib/auth/AuthProvider";

const geistSans = localFont({
  src: "./fonts/GeistVF.woff",
  variable: "--font-geist-sans",
  weight: "100 900",
});
const geistMono = localFont({
  src: "./fonts/GeistMonoVF.woff",
  variable: "--font-geist-mono",
  weight: "100 900",
});

export const metadata: Metadata = {
  title: "TaladPOS",
  description: "Point of Sale for a single fruit & general-goods store",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className={`${geistSans.variable} ${geistMono.variable} antialiased`}>
        <PrimeReactAppProvider>
          <AuthProvider>{children}</AuthProvider>
        </PrimeReactAppProvider>
      </body>
    </html>
  );
}
