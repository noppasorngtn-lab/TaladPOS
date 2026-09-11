import { test, expect } from "@playwright/test";
import {
  loginAsAdmin,
  loginAsCashier,
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

test.describe("Sales / Void", () => {
  test("Admin can void a just-completed order; stock is restored", async ({ page }) => {
    await loginAsAdmin(page);
    const name = uniqueName("void-me");
    await page.goto("/stock");
    await createProductViaUI(page, { name, price: "10", quantityOnHand: "5" });
    await page.goto("/sales");

    await checkoutOneUnit(page, name);
    await expect(page.getByText(/Sale completed/)).toBeVisible();

    await page.getByRole("button", { name: "Void", exact: true }).click();
    const dialog = page.getByRole("dialog", { name: "Void this order?" });
    await expect(dialog).toBeVisible();
    await dialog.getByRole("button", { name: "Void order" }).click();
    await expect(dialog).toBeHidden();

    await expect(page.getByRole("button", { name: "Void", exact: true })).toHaveCount(0);

    // The sales product list only refetches when the search box's value actually
    // changes; a hard reload guarantees a fresh fetch instead of stale pre-void state.
    // Scope the check to this product's own card — a generic "5 on hand" text match
    // collides with unrelated fixture products sharing the same round quantity.
    await page.reload();
    const card = await salesProductCard(page, name);
    await expect(card).toContainText("5 on hand");
  });

  test("Void button is absent for Cashier on a completed order", async ({ page }) => {
    await loginAsAdmin(page);
    const name = uniqueName("void-cashier-check");
    await page.goto("/stock");
    await createProductViaUI(page, { name, price: "10", quantityOnHand: "5" });

    await page.evaluate(() => localStorage.removeItem("taladpos.auth"));
    await loginAsCashier(page);
    await page.goto("/sales");
    await checkoutOneUnit(page, name);

    await expect(page.getByText(/Sale completed/)).toBeVisible();
    await expect(page.getByRole("button", { name: "Void", exact: true })).toHaveCount(0);
  });

  test("voiding the same order twice is rejected (409)", async ({ page, request }) => {
    await loginAsAdmin(page);
    const name = uniqueName("double-void");
    await page.goto("/stock");
    await createProductViaUI(page, { name, price: "10", quantityOnHand: "5" });
    await page.goto("/sales");

    const orderId = await checkoutOneUnit(page, name);

    await page.getByRole("button", { name: "Void", exact: true }).click();
    const dialog = page.getByRole("dialog", { name: "Void this order?" });
    await dialog.getByRole("button", { name: "Void order" }).click();
    await expect(dialog).toBeHidden();

    const { token } = await getAdminToken(request);
    const secondVoid = await request.post(`${API_BASE_URL}/sales-orders/${orderId}/void`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(secondVoid.status()).toBe(409);
  });
});
