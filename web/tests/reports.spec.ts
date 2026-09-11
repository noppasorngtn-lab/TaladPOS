import * as XLSX from "xlsx";
import { test, expect } from "@playwright/test";
import {
  loginAsAdmin,
  uniqueName,
  createProductViaUI,
  salesProductCard,
  getAdminToken,
  API_BASE_URL,
} from "./fixtures";

async function checkoutOneUnit(
  page: import("@playwright/test").Page,
  productName: string,
): Promise<string> {
  const card = await salesProductCard(page, productName);
  await card.click();
  const [response] = await Promise.all([
    page.waitForResponse((r) => r.url().includes("/sales-orders") && r.request().method() === "POST"),
    page.getByRole("button", { name: "Checkout", exact: true }).click(),
  ]);
  const body = await response.json();
  return body.id as string;
}

test.describe("Reports", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto("/reports");
  });

  test("all four report sections render with their headers", async ({ page }) => {
    await expect(page.getByRole("heading", { name: "Sales summary" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Best sellers" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Sales by staff" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Stock levels" })).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "Period" })).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "Product" }).first()).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "Staff" })).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "Qty on hand" })).toBeVisible();
  });

  test("granularity toggle switches between daily and monthly without error", async ({ page }) => {
    const granularitySelect = page.locator("select");
    await expect(granularitySelect).toHaveValue("daily");
    await granularitySelect.selectOption("monthly");
    await expect(granularitySelect).toHaveValue("monthly");
    await expect(page.getByRole("heading", { name: "Sales summary" })).toBeVisible();
  });

  test("stock levels reflects a freshly created product", async ({ page }) => {
    const name = uniqueName("report-stock");
    await page.goto("/stock");
    await createProductViaUI(page, { name, price: "10", quantityOnHand: "17" });

    await page.goto("/reports");
    const row = page.getByRole("row").filter({ hasText: name });
    await expect(row).toContainText("17");
  });

  test("stock-levels export downloads an .xlsx with the expected columns", async ({ page }) => {
    const [download] = await Promise.all([
      page.waitForEvent("download"),
      page.getByRole("button", { name: "Export", exact: true }).click(),
    ]);
    expect(download.suggestedFilename()).toMatch(/^stock-levels-\d{4}-\d{2}-\d{2}\.xlsx$/);

    const filePath = await download.path();
    const workbook = XLSX.readFile(filePath!);
    const sheet = workbook.Sheets[workbook.SheetNames[0]];
    const rows = XLSX.utils.sheet_to_json<string[]>(sheet, { header: 1 });
    const header = (rows[0] as string[]).join("|");
    expect(header).toMatch(/Product name/);
    expect(header).toMatch(/Quantity on hand/);
    expect(header).toMatch(/Low stock/);
    // At least one pre-existing Thai fixture product should round-trip correctly.
    const allText = rows.flat().join("|");
    expect(allText).toMatch(/[ก-๙]/);
  });
});

// Highest-value cross-cutting scenario in the suite: a voided order must vanish from
// every sales report and its stock must be restored. Runs as its own serial block —
// self-contained (creates its own product/order) so it isn't sensitive to what other
// specs are concurrently doing to the shared dev DB, and asserts presence/absence of
// its own uniquely-named product rather than any shared/absolute totals.
test.describe.serial("Reports / void exclusion", () => {
  let productName: string;
  let orderId: string;

  test.beforeAll(() => {
    productName = uniqueName("void-exclusion");
  });

  test("checkout appears in best-sellers and reduces stock", async ({ page, request }) => {
    await loginAsAdmin(page);
    await page.goto("/stock");
    await createProductViaUI(page, { name: productName, price: "10", quantityOnHand: "10" });
    await page.goto("/sales");
    orderId = await checkoutOneUnit(page, productName);

    await page.goto("/reports");
    // Stock-levels has no pagination cap (unlike best-sellers, which defaults to the
    // top 20 by volume and can't be relied on to surface a single ฿10 test sale once
    // the shared dev DB has many higher-value orders for "today") — use it as the
    // reliable UI-level signal that the checkout landed.
    const stockLevels = page.locator("section").filter({ hasText: "Stock levels" });
    await expect(
      stockLevels.getByRole("row").filter({ hasText: productName }),
    ).toContainText("9"); // 10 - 1 sold

    // Best-sellers' voided-exclusion rule (FR-033) is still verified directly against
    // the API with an explicit high limit, decoupled from the UI's unpaginated default.
    const { token } = await getAdminToken(request);
    const bestSellers = await request.get(
      `${API_BASE_URL}/reports/best-sellers?from=2020-01-01&to=2030-01-01&limit=1000`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    const { items } = await bestSellers.json();
    expect(items.some((i: { productName: string }) => i.productName === productName)).toBe(true);
  });

  test("voiding the order removes it from best-sellers and restores stock", async ({
    page,
    request,
  }) => {
    const { token } = await getAdminToken(request);
    const voidRes = await request.post(`${API_BASE_URL}/sales-orders/${orderId}/void`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(voidRes.status()).toBe(200);

    await loginAsAdmin(page);
    await page.goto("/reports");

    const stockLevels = page.locator("section").filter({ hasText: "Stock levels" });
    await expect(
      stockLevels.getByRole("row").filter({ hasText: productName }),
    ).toContainText("10"); // stock restored

    // Best sellers only lists products with completed sales in range — voided orders
    // are excluded server-side (FR-033), so this product drops out entirely.
    const bestSellers = await request.get(
      `${API_BASE_URL}/reports/best-sellers?from=2020-01-01&to=2030-01-01&limit=1000`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    const { items } = await bestSellers.json();
    expect(items.some((i: { productName: string }) => i.productName === productName)).toBe(false);
  });
});
