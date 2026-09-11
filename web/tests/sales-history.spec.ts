import * as XLSX from "xlsx";
import { test, expect } from "@playwright/test";
import {
  loginAsAdmin,
  uniqueName,
  createProductViaUI,
  salesProductCard,
} from "./fixtures";

function todayIso(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(
    now.getDate(),
  ).padStart(2, "0")}`;
}

async function checkoutOneUnit(
  page: import("@playwright/test").Page,
  productName: string,
): Promise<number> {
  const card = await salesProductCard(page, productName);
  await card.click();
  const [response] = await Promise.all([
    page.waitForResponse((r) => r.url().includes("/sales-orders") && r.request().method() === "POST"),
    page.getByRole("button", { name: "Checkout", exact: true }).click(),
  ]);
  const body = await response.json();
  return body.netTotal as number;
}

test.describe("Sales History", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("default view lists recent orders with a total count", async ({ page }) => {
    await page.goto("/sales-history");
    await expect(page.getByText(/\d+ total order\(s\)/)).toBeVisible();
  });

  test("read-only: no per-row actions column", async ({ page }) => {
    await page.goto("/sales-history");
    await expect(page.getByRole("columnheader", { name: "Actions" })).toHaveCount(0);
  });

  test("date range filter narrows results to a distinctive order", async ({ page }) => {
    const price = (900 + Math.floor(Math.random() * 90)).toFixed(2);
    const productName = uniqueName("history-date-filter");
    await page.goto("/stock");
    await createProductViaUI(page, { name: productName, price, quantityOnHand: "5" });
    await page.goto("/sales");
    await checkoutOneUnit(page, productName);

    const today = todayIso();
    await page.goto("/sales-history");
    await page.locator('input[type="date"]').nth(0).fill(today);
    await page.locator('input[type="date"]').nth(1).fill(today);
    await expect(page.getByText(`฿${price}`)).toBeVisible();

    // A range that excludes today should not show this order.
    await page.locator('input[type="date"]').nth(0).fill("2020-01-01");
    await page.locator('input[type="date"]').nth(1).fill("2020-01-02");
    await expect(page.getByText(`฿${price}`)).toHaveCount(0);
  });

  test("status filter separates Completed from Voided orders", async ({ page }) => {
    const price = (700 + Math.floor(Math.random() * 90)).toFixed(2);
    const productName = uniqueName("history-status-filter");
    await page.goto("/stock");
    await createProductViaUI(page, { name: productName, price, quantityOnHand: "5" });
    await page.goto("/sales");
    await checkoutOneUnit(page, productName);
    await page.getByRole("button", { name: "Void", exact: true }).click();
    await page
      .getByRole("dialog", { name: "Void this order?" })
      .getByRole("button", { name: "Void order" })
      .click();

    const today = todayIso();
    await page.goto("/sales-history");
    await page.locator('input[type="date"]').nth(0).fill(today);
    await page.locator('input[type="date"]').nth(1).fill(today);

    await page.locator("select").selectOption("Voided");
    await expect(page.getByText(`฿${price}`)).toBeVisible();

    await page.locator("select").selectOption("Completed");
    await expect(page.getByText(`฿${price}`)).toHaveCount(0);
  });

  test("clear filters resets the view", async ({ page }) => {
    await page.goto("/sales-history");
    await page.locator('input[type="date"]').nth(0).fill("2020-01-01");
    await page.locator('input[type="date"]').nth(1).fill("2020-01-02");
    await page.locator("select").selectOption("Voided");

    await page.getByRole("button", { name: "Clear filters" }).click();

    await expect(page.locator('input[type="date"]').nth(0)).toHaveValue("");
    await expect(page.locator('input[type="date"]').nth(1)).toHaveValue("");
    await expect(page.locator("select")).toHaveValue("");
  });

  test("export downloads an .xlsx with the expected filename and columns", async ({ page }) => {
    const today = todayIso();
    await page.goto("/sales-history");
    await page.locator('input[type="date"]').nth(0).fill(today);
    await page.locator('input[type="date"]').nth(1).fill(today);

    const [download] = await Promise.all([
      page.waitForEvent("download"),
      page.getByRole("button", { name: "Export", exact: true }).click(),
    ]);
    expect(download.suggestedFilename()).toBe(`sales-history-${today}-to-${today}.xlsx`);

    const filePath = await download.path();
    const workbook = XLSX.readFile(filePath!);
    const sheet = workbook.Sheets[workbook.SheetNames[0]];
    const rows = XLSX.utils.sheet_to_json<string[]>(sheet, { header: 1 });
    const header = (rows[0] as string[]).join("|");
    expect(header).toMatch(/Date/);
    expect(header).toMatch(/Staff/);
    expect(header).toMatch(/Member/);
    expect(header).toMatch(/Net total/);
    expect(header).toMatch(/Status/);
  });

  test("export with a filter matching nothing still downloads a header-only file", async ({
    page,
  }) => {
    await page.goto("/sales-history");
    await page.locator('input[type="date"]').nth(0).fill("2015-01-01");
    await page.locator('input[type="date"]').nth(1).fill("2015-01-02");

    const [download] = await Promise.all([
      page.waitForEvent("download"),
      page.getByRole("button", { name: "Export", exact: true }).click(),
    ]);

    const filePath = await download.path();
    const workbook = XLSX.readFile(filePath!);
    const sheet = workbook.Sheets[workbook.SheetNames[0]];
    const rows = XLSX.utils.sheet_to_json<string[]>(sheet, { header: 1 });
    expect(rows.length).toBe(1); // header row only, no data rows
  });

  test("Thai member names round-trip correctly in the exported file", async ({ page }) => {
    const price = (500 + Math.floor(Math.random() * 90)).toFixed(2);
    const productName = uniqueName("history-thai-export");
    const memberName = "ลูกค้าทดสอบ" + Date.now();
    const phone = `08${Date.now().toString().slice(-8)}`;

    await page.goto("/stock");
    await createProductViaUI(page, { name: productName, price, quantityOnHand: "5" });
    await page.goto("/sales");
    await page.getByPlaceholder("Phone number").fill(phone);
    await page.getByRole("button", { name: "Find" }).click();
    await page.getByRole("button", { name: "Sign up" }).click();
    const signupDialog = page.getByRole("dialog", { name: "Sign up member" });
    await signupDialog.locator("input:not([type])").nth(0).fill(memberName);
    await signupDialog.getByRole("button", { name: "Sign up", exact: true }).click();
    await expect(signupDialog).toBeHidden();
    await checkoutOneUnit(page, productName);

    const today = todayIso();
    await page.goto("/sales-history");
    await page.locator('input[type="date"]').nth(0).fill(today);
    await page.locator('input[type="date"]').nth(1).fill(today);

    const [download] = await Promise.all([
      page.waitForEvent("download"),
      page.getByRole("button", { name: "Export", exact: true }).click(),
    ]);
    const filePath = await download.path();
    const workbook = XLSX.readFile(filePath!);
    const sheet = workbook.Sheets[workbook.SheetNames[0]];
    const rows = XLSX.utils.sheet_to_json<string[]>(sheet, { header: 1 });
    const allText = rows.flat().join("|");
    expect(allText).toContain(memberName);
  });
});
