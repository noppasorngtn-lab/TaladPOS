import { test, expect } from "@playwright/test";
import {
  loginAsAdmin,
  uniqueName,
  createProductViaUI,
  salesProductCard,
  getAdminToken,
  API_BASE_URL,
} from "./fixtures";

function uniquePhone(): string {
  return `08${Date.now().toString().slice(-8)}${Math.floor(Math.random() * 100)}`;
}

test.describe("Sales / Members", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto("/sales");
  });

  test("looking up an unknown phone offers sign up or continue", async ({ page }) => {
    const phone = uniquePhone();
    await page.getByPlaceholder("Phone number").fill(phone);
    await page.getByRole("button", { name: "Find" }).click();

    await expect(page.getByText("No member with this phone number.")).toBeVisible();
    await expect(page.getByRole("button", { name: "Sign up" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Continue" })).toBeVisible();
  });

  test("continue without member dismisses the not-found banner", async ({ page }) => {
    const phone = uniquePhone();
    await page.getByPlaceholder("Phone number").fill(phone);
    await page.getByRole("button", { name: "Find" }).click();
    await page.getByRole("button", { name: "Continue" }).click();

    await expect(page.getByText("No member with this phone number.")).toHaveCount(0);
  });

  test("signing up a new member links them to the sale", async ({ page }) => {
    const phone = uniquePhone();
    const name = uniqueName("newmember");

    await page.getByPlaceholder("Phone number").fill(phone);
    await page.getByRole("button", { name: "Find" }).click();
    await page.getByRole("button", { name: "Sign up" }).click();

    const dialog = page.getByRole("dialog", { name: "Sign up member" });
    await expect(dialog).toBeVisible();
    await expect(dialog.locator("input:not([type])").nth(1)).toHaveValue(phone);
    await dialog.locator("input:not([type])").nth(0).fill(name);
    await dialog.getByRole("button", { name: "Sign up", exact: true }).click();
    await expect(dialog).toBeHidden();

    await expect(page.getByText(name)).toBeVisible();
    await expect(page.getByText(phone)).toBeVisible();
    await expect(page.getByRole("button", { name: "Unlink" })).toBeVisible();
  });

  test("signing up with a phone already in use is rejected", async ({ page }) => {
    const phone = uniquePhone();
    const first = uniqueName("first-owner");
    const second = uniqueName("second-owner");

    await page.getByPlaceholder("Phone number").fill(phone);
    await page.getByRole("button", { name: "Find" }).click();
    await page.getByRole("button", { name: "Sign up" }).click();
    let dialog = page.getByRole("dialog", { name: "Sign up member" });
    await dialog.locator("input:not([type])").nth(0).fill(first);
    await dialog.getByRole("button", { name: "Sign up", exact: true }).click();
    await expect(dialog).toBeHidden();

    await page.getByRole("button", { name: "Unlink" }).click();
    await page.getByPlaceholder("Phone number").fill(phone);
    await page.getByRole("button", { name: "Find" }).click();

    // Phone now belongs to `first`, so the lookup should link directly — force the
    // signup dialog open again via a *different*, still-unused phone number's
    // not-found flow, then retype the already-used phone inside the dialog itself.
    // Simpler: use a second not-found phone, open Sign up, then overwrite the phone
    // field with the already-registered number to trigger the duplicate-phone 422.
    const otherPhone = uniquePhone();
    await page.getByRole("button", { name: "Unlink" }).click();
    await page.getByPlaceholder("Phone number").fill(otherPhone);
    await page.getByRole("button", { name: "Find" }).click();
    await page.getByRole("button", { name: "Sign up" }).click();
    dialog = page.getByRole("dialog", { name: "Sign up member" });
    await dialog.locator("input:not([type])").nth(0).fill(second);
    await dialog.locator("input:not([type])").nth(1).fill(phone);
    await dialog.getByRole("button", { name: "Sign up", exact: true }).click();

    await expect(dialog.getByText(/already/i)).toBeVisible();
    await expect(dialog).toBeVisible();
  });

  test("looking up an existing member links directly without the signup dialog", async ({
    page,
  }) => {
    const phone = uniquePhone();
    const name = uniqueName("existing");

    await page.getByPlaceholder("Phone number").fill(phone);
    await page.getByRole("button", { name: "Find" }).click();
    await page.getByRole("button", { name: "Sign up" }).click();
    const dialog = page.getByRole("dialog", { name: "Sign up member" });
    await dialog.locator("input:not([type])").nth(0).fill(name);
    await dialog.getByRole("button", { name: "Sign up", exact: true }).click();
    await expect(dialog).toBeHidden();
    await page.getByRole("button", { name: "Unlink" }).click();

    await page.getByPlaceholder("Phone number").fill(phone);
    await page.getByRole("button", { name: "Find" }).click();

    await expect(page.getByRole("dialog", { name: "Sign up member" })).toBeHidden();
    await expect(page.getByText(name)).toBeVisible();
    await expect(page.getByRole("button", { name: "Unlink" })).toBeVisible();
  });

  test("unlink clears the linked member", async ({ page }) => {
    const phone = uniquePhone();
    const name = uniqueName("unlink-me");

    await page.getByPlaceholder("Phone number").fill(phone);
    await page.getByRole("button", { name: "Find" }).click();
    await page.getByRole("button", { name: "Sign up" }).click();
    const dialog = page.getByRole("dialog", { name: "Sign up member" });
    await dialog.locator("input:not([type])").nth(0).fill(name);
    await dialog.getByRole("button", { name: "Sign up", exact: true }).click();
    await expect(dialog).toBeHidden();

    await page.getByRole("button", { name: "Unlink" }).click();
    await expect(page.getByRole("button", { name: "Unlink" })).toHaveCount(0);
    await expect(page.getByPlaceholder("Phone number")).toHaveValue("");
  });

  test("checkout with a linked member credits their accumulated total", async ({
    page,
    request,
  }) => {
    const phone = uniquePhone();
    const memberName = uniqueName("credit-member");
    const productName = uniqueName("member-checkout-product");

    await page.goto("/stock");
    await createProductViaUI(page, { name: productName, price: "30", quantityOnHand: "5" });
    await page.goto("/sales");

    await page.getByPlaceholder("Phone number").fill(phone);
    await page.getByRole("button", { name: "Find" }).click();
    await page.getByRole("button", { name: "Sign up" }).click();
    const signupDialog = page.getByRole("dialog", { name: "Sign up member" });
    await signupDialog.locator("input:not([type])").nth(0).fill(memberName);
    await signupDialog.getByRole("button", { name: "Sign up", exact: true }).click();
    await expect(signupDialog).toBeHidden();

    const { token } = await getAdminToken(request);
    const beforeRes = await request.get(
      `${API_BASE_URL}/members?phone=${encodeURIComponent(phone)}`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    const before = await beforeRes.json();
    expect(before.accumulatedPurchaseTotal).toBe(0);

    const card = await salesProductCard(page, productName);
    await card.click();
    // Read the actual net total from the checkout response rather than assuming ฿30 flat —
    // an unrelated test elsewhere in the suite may have an active MemberDiscount-scope
    // promotion running (it's a global scope, not tied to any one member), which would
    // legitimately discount this checkout too. The credited amount must match whatever
    // the order actually charged, not a hardcoded no-discount expectation.
    const [checkoutResponse] = await Promise.all([
      page.waitForResponse((r) => r.url().includes("/sales-orders") && r.request().method() === "POST"),
      page.getByRole("button", { name: "Checkout", exact: true }).click(),
    ]);
    const order = await checkoutResponse.json();
    await expect(page.getByText(`Sale completed — total ฿${order.netTotal.toFixed(2)}`)).toBeVisible();

    const afterRes = await request.get(`${API_BASE_URL}/members/${before.id}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    const after = await afterRes.json();
    expect(after.accumulatedPurchaseTotal).toBe(before.accumulatedPurchaseTotal + order.netTotal);
  });
});
