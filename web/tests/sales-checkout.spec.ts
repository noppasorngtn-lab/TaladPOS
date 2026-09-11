import { test, expect } from "@playwright/test";
import {
  loginAsAdmin,
  loginAsCashier,
  uniqueName,
  createProductViaUI,
  productRow,
  salesProductCard,
  cartLine,
  cartIncreaseButton,
  cartDecreaseButton,
  cartRemoveButton,
} from "./fixtures";

async function setupProduct(
  page: import("@playwright/test").Page,
  opts: Parameters<typeof createProductViaUI>[1],
) {
  await page.goto("/stock");
  await createProductViaUI(page, opts);
}

test.describe("Sales / Checkout", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("clicking a product card adds it to the cart", async ({ page }) => {
    const name = uniqueName("cart-add");
    await setupProduct(page, { name, price: "12", quantityOnHand: "5" });
    await page.goto("/sales");
    const card = await salesProductCard(page, name);
    await card.click();

    const line = cartLine(page, name);
    await expect(line).toBeVisible();
    await expect(line.locator("span.w-6")).toHaveText("1");
  });

  test("clicking the same card again increments quantity, capped at stock", async ({ page }) => {
    const name = uniqueName("cart-cap");
    await setupProduct(page, { name, price: "12", quantityOnHand: "2" });
    await page.goto("/sales");
    const card = await salesProductCard(page, name);
    await card.click();
    await card.click();

    const line = cartLine(page, name);
    await expect(line.locator("span.w-6")).toHaveText("2");
    await expect(cartIncreaseButton(line)).toBeDisabled();
  });

  test("decreasing to zero removes the line; trash removes regardless of quantity", async ({
    page,
  }) => {
    const name = uniqueName("cart-remove");
    await setupProduct(page, { name, price: "12", quantityOnHand: "5" });
    await page.goto("/sales");
    const card = await salesProductCard(page, name);

    await card.click();
    await cartDecreaseButton(cartLine(page, name)).click();
    await expect(cartLine(page, name)).toHaveCount(0);

    await card.click();
    await card.click();
    await cartRemoveButton(cartLine(page, name)).click();
    await expect(cartLine(page, name)).toHaveCount(0);
  });

  test("pricing preview shows a subtotal once items are in the cart", async ({ page }) => {
    const name = uniqueName("cart-pricing");
    await setupProduct(page, { name, price: "15", quantityOnHand: "5" });
    await page.goto("/sales");
    const card = await salesProductCard(page, name);
    await card.click();
    await expect(page.getByText("Subtotal")).toBeVisible();
  });

  test("checkout is disabled with an empty cart", async ({ page }) => {
    await page.goto("/sales");
    await expect(page.getByRole("button", { name: "Checkout", exact: true })).toBeDisabled();
  });

  test("happy-path checkout completes the sale and decrements stock", async ({ page }) => {
    const name = uniqueName("cart-checkout");
    await setupProduct(page, { name, price: "20", quantityOnHand: "5" });
    await page.goto("/sales");
    const card = await salesProductCard(page, name);
    await card.click();
    await card.click(); // qty 2 @ ฿20 = ฿40.00

    await page.getByRole("button", { name: "Checkout", exact: true }).click();
    await expect(page.getByText(/Sale completed — total ฿40\.00/)).toBeVisible();

    // Scoped to this product's own card — a generic "N on hand" text match can collide
    // with unrelated fixture products sharing the same round quantity.
    const cardAfterCheckout = await salesProductCard(page, name);
    await expect(cardAfterCheckout).toContainText("3 on hand");
  });

  test("barcode search surfaces the matching product card", async ({ page }) => {
    const name = uniqueName("cart-barcode");
    const barcode = uniqueName("barcode");
    await setupProduct(page, { name, price: "10", quantityOnHand: "5", barcode });
    await page.goto("/sales");
    // The ProductCard only renders the product's name/price, never its barcode —
    // search BY the barcode value, then assert the card that surfaces shows the name.
    await page.getByPlaceholder("Search by name or barcode...").fill(barcode);
    const card = page.getByRole("button").filter({ hasText: name });
    await expect(card).toBeVisible();
  });

  test("insufficient stock at checkout aborts the whole transaction", async ({ page, browser }) => {
    const name = uniqueName("cart-race");
    await setupProduct(page, { name, price: "10", quantityOnHand: "2" });
    await page.goto("/sales");
    const card = await salesProductCard(page, name);
    await card.click();
    await card.click(); // cart qty 2, matches current stock exactly

    // Out-of-band: a second Admin session drops stock below the cart's held quantity
    // so the checkout in the first context is now genuinely over-stock (FR-005/FR-006).
    const context2 = await browser.newContext();
    const page2 = await context2.newPage();
    await loginAsAdmin(page2);
    await page2.goto("/stock");
    const row2 = await productRow(page2, name);
    await row2.getByRole("button", { name: "Edit" }).click();
    const dialog2 = page2.getByRole("dialog", { name: "Edit product" });
    await dialog2.locator('input[type="number"]').nth(1).fill("0");
    await dialog2.getByRole("button", { name: "Save" }).click();
    await expect(dialog2).toBeHidden();
    await context2.close();

    await page.getByRole("button", { name: "Checkout", exact: true }).click();
    await expect(page.getByText(/is less than the requested quantity of/)).toBeVisible();
    await expect(cartLine(page, name)).toBeVisible();
    await expect(page.getByText(/Sale completed/)).toHaveCount(0);
  });

  test("Cashier can search a product and complete a checkout", async ({ page }) => {
    const name = uniqueName("cashier-checkout");
    await setupProduct(page, { name, price: "10", quantityOnHand: "5" });

    await page.evaluate(() => localStorage.removeItem("taladpos.auth"));
    await loginAsCashier(page);
    await page.goto("/sales");
    const card = await salesProductCard(page, name);
    await card.click();
    await page.getByRole("button", { name: "Checkout", exact: true }).click();
    await expect(page.getByText(/Sale completed/)).toBeVisible();
  });
});
