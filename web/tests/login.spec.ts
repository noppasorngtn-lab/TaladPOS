import { test, expect } from "@playwright/test";
import { ADMIN_USERNAME, ADMIN_PASSWORD, CASHIER_USERNAME, CASHIER_PASSWORD } from "./fixtures";

test.describe("Login", () => {
  test("valid Admin login redirects to /stock", async ({ page }) => {
    await page.goto("/login");
    await page.getByRole("textbox", { name: "Username" }).fill(ADMIN_USERNAME);
    await page.getByRole("textbox", { name: "Password" }).fill(ADMIN_PASSWORD);
    await page.getByRole("button", { name: "Sign in" }).click();
    await page.waitForURL((url) => url.pathname === "/stock");
  });

  test("valid Cashier login redirects to /sales", async ({ page }) => {
    await page.goto("/login");
    await page.getByRole("textbox", { name: "Username" }).fill(CASHIER_USERNAME);
    await page.getByRole("textbox", { name: "Password" }).fill(CASHIER_PASSWORD);
    await page.getByRole("button", { name: "Sign in" }).click();
    await page.waitForURL((url) => url.pathname === "/sales");
  });

  test("invalid credentials show an inline error and do not navigate", async ({ page }) => {
    await page.goto("/login");
    await page.getByRole("textbox", { name: "Username" }).fill("admin");
    await page.getByRole("textbox", { name: "Password" }).fill("wrong-password");
    await page.getByRole("button", { name: "Sign in" }).click();
    await expect(page.getByText("Invalid username or password.")).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
  });

  test("unauthenticated access to a protected route redirects to /login", async ({ page }) => {
    await page.goto("/stock");
    await page.waitForURL((url) => url.pathname === "/login");
  });
});
