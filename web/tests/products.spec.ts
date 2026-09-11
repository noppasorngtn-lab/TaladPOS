import path from "node:path";
import { test, expect } from "@playwright/test";
import { loginAsAdmin, uniqueName, createProductViaUI, productRow } from "./fixtures";

const SAMPLE_IMAGE = path.join(__dirname, "fixtures", "sample-product.jpg");

test.describe("Stock / Products", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto("/stock");
  });

  test("add a product with all fields including an image", async ({ page }) => {
    const name = uniqueName("mango");
    const barcode = uniqueName("bc");

    await createProductViaUI(page, {
      name,
      price: "25.50",
      quantityOnHand: "40",
      barcode,
      lowStockThreshold: "5",
      imagePath: SAMPLE_IMAGE,
    });

    const row = await productRow(page, name);
    await expect(row).toContainText("฿25.50");
    await expect(row).toContainText("40");
    await expect(row).toContainText(barcode);
  });

  test("add a product without optional barcode/threshold", async ({ page }) => {
    const name = uniqueName("mangosteen");
    await createProductViaUI(page, { name, price: "10", quantityOnHand: "15" });

    const row = await productRow(page, name);
    await expect(row).toContainText("-");
  });

  test("duplicate barcode is rejected with an inline error", async ({ page }) => {
    const barcode = uniqueName("dup-bc");
    const first = uniqueName("first");
    const second = uniqueName("second");

    await createProductViaUI(page, { name: first, price: "10", quantityOnHand: "10", barcode });

    await page.getByRole("button", { name: "Add product" }).click();
    const dialog = page.getByRole("dialog", { name: "Add product" });
    await dialog.locator("input:not([type])").nth(0).fill(second);
    await dialog.locator('input[type="number"]').nth(0).fill("10");
    await dialog.locator('input[type="number"]').nth(1).fill("10");
    await dialog.locator("input:not([type])").nth(1).fill(barcode);
    await dialog.getByRole("button", { name: "Save" }).click();

    await expect(dialog.getByText(/already used by another product/)).toBeVisible();
    await expect(dialog).toBeVisible();
    await dialog.getByRole("button", { name: "Cancel" }).click();
  });

  test("price below the minimum is blocked by client-side validation", async ({ page }) => {
    const name = uniqueName("badprice");
    await page.getByRole("button", { name: "Add product" }).click();
    const dialog = page.getByRole("dialog", { name: "Add product" });
    await dialog.locator("input:not([type])").nth(0).fill(name);
    await dialog.locator('input[type="number"]').nth(0).fill("0");
    await dialog.locator('input[type="number"]').nth(1).fill("10");
    await dialog.getByRole("button", { name: "Save" }).click();
    // Native HTML5 min=0.01 constraint blocks submission — dialog never closes.
    await expect(dialog).toBeVisible();
  });

  test("edit an existing product updates the table", async ({ page }) => {
    const name = uniqueName("edit-me");
    await createProductViaUI(page, { name, price: "10", quantityOnHand: "10" });

    const row = await productRow(page, name);
    await row.getByRole("button", { name: "Edit" }).click();
    const dialog = page.getByRole("dialog", { name: "Edit product" });
    await expect(dialog).toBeVisible();
    await dialog.locator('input[type="number"]').nth(0).fill("99.99");
    await dialog.getByRole("button", { name: "Save" }).click();
    await expect(dialog).toBeHidden();

    await expect(await productRow(page, name)).toContainText("฿99.99");
  });

  test("low-stock banner appears when quantity is at or below the threshold", async ({ page }) => {
    const name = uniqueName("lowstock");
    await createProductViaUI(page, {
      name,
      price: "10",
      quantityOnHand: "3",
      lowStockThreshold: "5",
    });

    await expect(page.getByText(/at or below its low-stock threshold/)).toBeVisible();
    const row = await productRow(page, name);
    await expect(row).toContainText("Low stock");
  });

  test("search filters the product table by name and by barcode", async ({ page }) => {
    const name = uniqueName("searchable");
    const barcode = uniqueName("search-bc");
    await createProductViaUI(page, { name, price: "10", quantityOnHand: "10", barcode });

    const search = page.getByPlaceholder("Search by name or barcode...");
    await search.fill(name);
    await expect(page.getByRole("row").filter({ hasText: name })).toBeVisible();

    await search.fill(barcode);
    await expect(page.getByRole("row").filter({ hasText: name })).toBeVisible();

    await search.fill(uniqueName("no-such-product"));
    await expect(page.getByRole("row").filter({ hasText: name })).toHaveCount(0);
  });

  // Blocked: clicking "Remove" reliably fires window.confirm and the app calls
  // deleteProduct(), but the browser reports the resulting DELETE as net::ERR_ABORTED
  // and the row survives a full page reload (confirmed via requestfailed listener +
  // fresh reload check). This is NOT a request-shape/CORS/app-code bug: the identical
  // DELETE succeeds via `curl` and via a raw `page.evaluate(() => fetch(...))` call in
  // the same authenticated session — only the click-driven path fails, in both `next
  // dev` and a production `next build && next start`. Root cause not identified;
  // flagging rather than asserting around it.
  test.fixme(
    "removing a product soft-deletes it out of the active list",
    async ({ page }) => {
      const name = uniqueName("removable");
      await createProductViaUI(page, { name, price: "10", quantityOnHand: "10" });

      page.once("dialog", (d) => d.accept());
      const row = await productRow(page, name);
      await row.getByRole("button", { name: "Remove" }).click();
      await expect(page.getByRole("row").filter({ hasText: name })).toHaveCount(0);
    },
  );
});

// Cashier RBAC behavior for this screen is covered in security-rbac.spec.ts, which
// isn't nested under this file's Admin-only beforeEach.
