import { test, expect } from "@playwright/test";
import {
  loginAsAdmin,
  uniqueName,
  uniquePercent,
  uniqueFutureDateRange,
  createProductViaUI,
  salesProductCard,
} from "./fixtures";

// KNOWN LIMITATION: PromotionFormDialog's Product <select> loads via
// searchProducts(token, "") with no search box of its own, so it only ever shows the
// API's default first page (20 products, alphabetical). Tests below that need to pick
// a *specific* product (by "มังคุด" or a freshly created one) will fail with a
// selectOption timeout once the shared dev DB accumulates 20+ products whose names sort
// ahead of the target — which happens quickly across repeated full-suite runs, since
// most fixture products end up referenced by a SalesOrderLine or Promotion (FK RESTRICT)
// and can't be hard-deleted afterward. Run the cleanup below before a full-suite run to
// keep this file passing:
//   docker exec taladpos-postgres psql -U postgres -d taladpos -c \
//     'DELETE FROM "Products" WHERE "Name" LIKE '"'"'E2E-%'"'"' \
//      AND "Id" NOT IN (SELECT "ProductId" FROM "SalesOrderLines") \
//      AND "Id" NOT IN (SELECT "ProductId" FROM "Promotions" WHERE "ProductId" IS NOT NULL);'
test.describe("Promotions", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("create a per-product promotion with a 90% discount", async ({ page }) => {
    await page.goto("/promotions");

    // Idempotency: deactivate any leftover active มังคุด promotion from a previous run
    // so the row-lookup below always matches exactly one row.
    page.on("dialog", (dialog) => dialog.accept());
    const activeMangosteenRow = page
      .getByRole("row")
      .filter({ hasText: "มังคุด" })
      .filter({ hasText: "Active" });
    while ((await activeMangosteenRow.count()) > 0) {
      await activeMangosteenRow.first().getByRole("button", { name: "Deactivate" }).click();
      await expect(activeMangosteenRow).toHaveCount(0);
    }

    await page.getByRole("button", { name: "Add promotion" }).click();

    const dialog = page.getByRole("dialog", { name: "Add promotion" });
    await expect(dialog).toBeVisible();

    // Scope defaults to "Per product"; pick a product for it.
    const productSelect = dialog.locator("select").nth(1);
    await productSelect.selectOption({ label: "มังคุด" });

    const discountInput = dialog.locator('input[type="number"]');
    await discountInput.fill("90");

    const startDate = dialog.locator('input[type="date"]').nth(0);
    const endDate = dialog.locator('input[type="date"]').nth(1);
    await startDate.fill("2026-09-11");
    await endDate.fill("2026-09-30");

    await dialog.getByRole("button", { name: "Save" }).click();
    await expect(dialog).toBeHidden();

    const row = page.getByRole("row").filter({ hasText: "มังคุด" }).filter({ hasText: "Active" });
    await expect(row).toHaveCount(1);
    await expect(row).toContainText("90%");

    await page.screenshot({
      path: "test-results/screenshots/promotion-90-percent-created.png",
      fullPage: true,
    });
  });

  test("whole-bill scope has no product field and shows '-' for Product", async ({ page }) => {
    await page.goto("/promotions");
    await page.getByRole("button", { name: "Add promotion" }).click();
    const dialog = page.getByRole("dialog", { name: "Add promotion" });

    await dialog.locator("select").nth(0).selectOption("WholeBill");
    await expect(dialog.locator("select")).toHaveCount(1); // Product select is gone

    const { start } = uniqueFutureDateRange();
    await dialog.locator('input[type="number"]').fill(uniquePercent(15));
    await dialog.locator('input[type="date"]').nth(0).fill(start);
    await dialog.locator('input[type="date"]').nth(1).fill(start);
    await dialog.getByRole("button", { name: "Save" }).click();
    await expect(dialog).toBeHidden();

    const row = page
      .getByRole("row")
      .filter({ hasText: "Whole bill" })
      .filter({ hasText: start });
    await expect(row).toContainText("-");
  });

  test("member-discount scope has no product field either", async ({ page }) => {
    await page.goto("/promotions");
    await page.getByRole("button", { name: "Add promotion" }).click();
    const dialog = page.getByRole("dialog", { name: "Add promotion" });

    await dialog.locator("select").nth(0).selectOption("MemberDiscount");
    await expect(dialog.locator("select")).toHaveCount(1);

    const { start } = uniqueFutureDateRange();
    await dialog.locator('input[type="number"]').fill(uniquePercent(7));
    await dialog.locator('input[type="date"]').nth(0).fill(start);
    await dialog.locator('input[type="date"]').nth(1).fill(start);
    await dialog.getByRole("button", { name: "Save" }).click();
    await expect(dialog).toBeHidden();

    const row = page
      .getByRole("row")
      .filter({ hasText: "Member discount" })
      .filter({ hasText: start });
    await expect(row).toContainText("-");
  });

  test("switching scope away from Per product and back resets the product field", async ({
    page,
  }) => {
    await page.goto("/promotions");
    await page.getByRole("button", { name: "Add promotion" }).click();
    const dialog = page.getByRole("dialog", { name: "Add promotion" });

    const scopeSelect = dialog.locator("select").nth(0);
    await dialog.locator("select").nth(1).selectOption({ label: "มังคุด" });
    await scopeSelect.selectOption("WholeBill");
    await scopeSelect.selectOption("PerProduct");

    const productSelect = dialog.locator("select").nth(1);
    await expect(productSelect).toHaveValue("");
  });

  test("end date before start date is rejected", async ({ page }) => {
    await page.goto("/promotions");
    await page.getByRole("button", { name: "Add promotion" }).click();
    const dialog = page.getByRole("dialog", { name: "Add promotion" });

    await dialog.locator("select").nth(0).selectOption("WholeBill");
    await dialog.locator('input[type="number"]').fill("10");
    await dialog.locator('input[type="date"]').nth(0).fill("2026-09-20");
    await dialog.locator('input[type="date"]').nth(1).fill("2026-09-10");
    await dialog.getByRole("button", { name: "Save" }).click();

    await expect(dialog.getByText(/EndDate must be on or after StartDate/)).toBeVisible();
    await expect(dialog).toBeVisible();
  });

  test("discount percent outside 0.01–100 is blocked by client-side validation", async ({
    page,
  }) => {
    await page.goto("/promotions");
    await page.getByRole("button", { name: "Add promotion" }).click();
    const dialog = page.getByRole("dialog", { name: "Add promotion" });

    await dialog.locator("select").nth(0).selectOption("WholeBill");
    await dialog.locator('input[type="date"]').nth(0).fill("2026-09-01");
    await dialog.locator('input[type="date"]').nth(1).fill("2026-09-30");

    await dialog.locator('input[type="number"]').fill("101");
    await dialog.getByRole("button", { name: "Save" }).click();
    await expect(dialog).toBeVisible(); // native max=100 constraint blocks submission

    await dialog.locator('input[type="number"]').fill("0");
    await dialog.getByRole("button", { name: "Save" }).click();
    await expect(dialog).toBeVisible(); // native min=0.01 constraint blocks submission
  });

  test("editing an existing promotion updates its discount percent", async ({ page }) => {
    await page.goto("/promotions");
    await page.getByRole("button", { name: "Add promotion" }).click();
    let dialog = page.getByRole("dialog", { name: "Add promotion" });
    await dialog.locator("select").nth(0).selectOption("WholeBill");
    const { start } = uniqueFutureDateRange();
    await dialog.locator('input[type="number"]').fill(uniquePercent(20));
    await dialog.locator('input[type="date"]').nth(0).fill(start);
    await dialog.locator('input[type="date"]').nth(1).fill(start);
    await dialog.getByRole("button", { name: "Save" }).click();
    await expect(dialog).toBeHidden();

    // The date is unique per run and unaffected by the edit, so it's a stable
    // identifier for this row both before and after changing the discount percent.
    const row = page.getByRole("row").filter({ hasText: "Whole bill" }).filter({ hasText: start });
    await row.getByRole("button", { name: "Edit" }).click();
    dialog = page.getByRole("dialog", { name: "Edit promotion" });
    await expect(dialog).toBeVisible();
    const updatedPercent = uniquePercent(25);
    await dialog.locator('input[type="number"]').fill(updatedPercent);
    await dialog.getByRole("button", { name: "Save" }).click();
    await expect(dialog).toBeHidden();

    await expect(row).toContainText(`${updatedPercent}%`);
  });

  test("deactivating a promotion marks it inactive but keeps its history", async ({ page }) => {
    await page.goto("/promotions");
    await page.getByRole("button", { name: "Add promotion" }).click();
    const dialog = page.getByRole("dialog", { name: "Add promotion" });
    await dialog.locator("select").nth(0).selectOption("WholeBill");
    const { start } = uniqueFutureDateRange();
    await dialog.locator('input[type="number"]').fill(uniquePercent(33));
    await dialog.locator('input[type="date"]').nth(0).fill(start);
    await dialog.locator('input[type="date"]').nth(1).fill(start);
    await dialog.getByRole("button", { name: "Save" }).click();
    await expect(dialog).toBeHidden();

    page.once("dialog", (d) => d.accept());
    const row = page.getByRole("row").filter({ hasText: "Whole bill" }).filter({ hasText: start });
    await row.getByRole("button", { name: "Deactivate" }).click();

    await expect(row).toContainText("Deactivated");
    await expect(row.getByRole("button", { name: "Deactivate" })).toHaveCount(0);
  });

  test("a per-product promotion and a member discount stack sequentially at checkout", async ({
    page,
  }) => {
    const productName = uniqueName("combo-discount-product");

    await page.goto("/stock");
    await createProductViaUI(page, { name: productName, price: "100", quantityOnHand: "5" });

    await page.goto("/promotions");

    await page.getByRole("button", { name: "Add promotion" }).click();
    let dialog = page.getByRole("dialog", { name: "Add promotion" });
    await dialog.locator("select").nth(1).selectOption({ label: productName });
    await dialog.locator('input[type="number"]').fill("10");
    await dialog.locator('input[type="date"]').nth(0).fill("2026-01-01");
    await dialog.locator('input[type="date"]').nth(1).fill("2026-12-31");
    await dialog.getByRole("button", { name: "Save" }).click();
    await expect(dialog).toBeHidden();

    await page.getByRole("button", { name: "Add promotion" }).click();
    dialog = page.getByRole("dialog", { name: "Add promotion" });
    await dialog.locator("select").nth(0).selectOption("MemberDiscount");
    await dialog.locator('input[type="number"]').fill("5");
    await dialog.locator('input[type="date"]').nth(0).fill("2026-01-01");
    await dialog.locator('input[type="date"]').nth(1).fill("2026-12-31");
    await dialog.getByRole("button", { name: "Save" }).click();
    await expect(dialog).toBeHidden();

    const phone = `08${Date.now().toString().slice(-8)}`;
    const memberName = uniqueName("combo-member");
    await page.goto("/sales");
    await page.getByPlaceholder("Phone number").fill(phone);
    await page.getByRole("button", { name: "Find" }).click();
    await page.getByRole("button", { name: "Sign up" }).click();
    const signupDialog = page.getByRole("dialog", { name: "Sign up member" });
    await signupDialog.locator("input:not([type])").nth(0).fill(memberName);
    await signupDialog.getByRole("button", { name: "Sign up", exact: true }).click();
    await expect(signupDialog).toBeHidden();

    const card = await salesProductCard(page, productName);
    await card.click();

    // ฿100 subtotal → 10% product promo (-฿10) → ฿90 remainder → 5% member discount
    // on the remainder (-฿4.50) → ฿85.50 net. FR-024: shown as two separate lines.
    await expect(page.getByText("Subtotal")).toBeVisible();
    await expect(page.getByText("Promotion discount")).toBeVisible();
    await expect(page.getByText("-฿10.00")).toBeVisible();
    await expect(page.getByText("Member discount")).toBeVisible();
    await expect(page.getByText("-฿4.50")).toBeVisible();
    await expect(page.getByText("฿85.50")).toBeVisible();
  });
});
