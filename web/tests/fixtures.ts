import type { APIRequestContext, Locator, Page } from "@playwright/test";
import { expect } from "@playwright/test";

export const ADMIN_USERNAME = "admin";
export const ADMIN_PASSWORD = "Admin@12345";
export const CASHIER_USERNAME = "e2e-cashier";
export const CASHIER_PASSWORD = "Admin@12345";

const API_BASE_URL = "http://localhost:5260";

async function login(page: Page, username: string, password: string): Promise<void> {
  await page.goto("/login");
  await page.getByRole("textbox", { name: "Username" }).fill(username);
  await page.getByRole("textbox", { name: "Password" }).fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await page.waitForURL((url) => url.pathname !== "/login");
}

export function loginAsAdmin(page: Page): Promise<void> {
  return login(page, ADMIN_USERNAME, ADMIN_PASSWORD);
}

export function loginAsCashier(page: Page): Promise<void> {
  return login(page, CASHIER_USERNAME, CASHIER_PASSWORD);
}

/** Collision-free name for fixture data created by parallel test runs. */
export function uniqueName(prefix: string): string {
  return `E2E-${Date.now()}-${Math.floor(Math.random() * 10000)}-${prefix}`;
}

/**
 * Promotions have no name field — tests that filter the promotions table by scope +
 * discount% (rather than reusing a fixture row) need a percent value that won't collide
 * with rows left over from a previous/parallel run. Adds a small random fraction to a
 * round base percent (stays within the 0.01–100 input range for any base <= 99).
 */
export function uniquePercent(base: number): string {
  return (base + Math.random() * 0.9 + 0.01).toFixed(2);
}

/**
 * A pseudo-unique single-day date range far enough in the future (year 2030+) that it
 * can't collide with any hand-picked fixed date used elsewhere in this suite. Dates,
 * unlike discount percent, stay constant across an edit — use this as the stable
 * identifier for locating a promotion row both before and after editing it.
 */
export function uniqueFutureDateRange(): { start: string; end: string } {
  const anchor = new Date(Date.UTC(2030, 0, 1));
  anchor.setUTCDate(anchor.getUTCDate() + Math.floor(Math.random() * 3650));
  const date = anchor.toISOString().slice(0, 10);
  return { start: date, end: date };
}

interface AuthToken {
  token: string;
  staffId: string;
}

async function getToken(
  request: APIRequestContext,
  username: string,
  password: string,
): Promise<AuthToken> {
  const response = await request.post(`${API_BASE_URL}/auth/login`, {
    data: { username, password },
  });
  const body = await response.json();
  return { token: body.token as string, staffId: body.staff.id as string };
}

export function getAdminToken(request: APIRequestContext): Promise<AuthToken> {
  return getToken(request, ADMIN_USERNAME, ADMIN_PASSWORD);
}

export function getCashierToken(request: APIRequestContext): Promise<AuthToken> {
  return getToken(request, CASHIER_USERNAME, CASHIER_PASSWORD);
}

export { API_BASE_URL };

/**
 * GET /products defaults to a 20-item page and the Stock screen never paginates past
 * that — with 45+ fixture products accumulated in the shared dev DB, a newly created
 * row is frequently NOT in the unfiltered table. Always search by the exact unique
 * name before asserting on a row; this scopes the query to that one product and sinks
 * the pagination cap entirely, regardless of how large the table has grown.
 */
export async function productRow(page: Page, name: string): Promise<Locator> {
  await page.getByPlaceholder("Search by name or barcode...").fill(name);
  const row = page.getByRole("row").filter({ hasText: name });
  await expect(row).toBeVisible();
  return row;
}

export interface CreateProductOptions {
  name: string;
  price: string;
  quantityOnHand: string;
  barcode?: string;
  lowStockThreshold?: string;
  imagePath?: string;
}

/** Fills and submits the "Add product" dialog on the Stock screen (/stock). */
export async function createProductViaUI(page: Page, options: CreateProductOptions): Promise<void> {
  await page.getByRole("button", { name: "Add product" }).click();
  const dialog = page.getByRole("dialog", { name: "Add product" });
  await dialog.locator("input:not([type])").nth(0).fill(options.name);
  await dialog.locator('input[type="number"]').nth(0).fill(options.price);
  await dialog.locator('input[type="number"]').nth(1).fill(options.quantityOnHand);
  if (options.barcode) {
    await dialog.locator("input:not([type])").nth(1).fill(options.barcode);
  }
  if (options.lowStockThreshold) {
    await dialog.locator('input[type="number"]').nth(2).fill(options.lowStockThreshold);
  }
  if (options.imagePath) {
    await dialog.locator('input[type="file"]').setInputFiles(options.imagePath);
  }
  await dialog.getByRole("button", { name: "Save" }).click();
  await expect(dialog).toBeHidden();
}

/**
 * GET /products defaults to a 20-item page on the Sales screen too (empty search =
 * unfiltered first page). Always type the exact product name into the search box
 * before looking for its card, otherwise a fixture product created after the 20th
 * row simply won't render.
 */
export async function salesProductCard(page: Page, name: string): Promise<Locator> {
  await page.getByPlaceholder("Search by name or barcode...").fill(name);
  const card = page.getByRole("button").filter({ hasText: name });
  await expect(card).toBeVisible();
  return card;
}

/** The cart line `<li>` for a given product name (Cart.tsx renders no name/label on its +/-/trash icon buttons). */
export function cartLine(page: Page, name: string): Locator {
  return page.locator("li").filter({ hasText: name });
}

export function cartIncreaseButton(line: Locator): Locator {
  return line.locator("button:has(span.pi-plus)");
}

export function cartDecreaseButton(line: Locator): Locator {
  return line.locator("button:has(span.pi-minus)");
}

export function cartRemoveButton(line: Locator): Locator {
  return line.locator("button:has(span.pi-trash)");
}
