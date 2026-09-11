import { test, expect } from "@playwright/test";
import {
  loginAsAdmin,
  loginAsCashier,
  getAdminToken,
  getCashierToken,
  createProductViaUI,
  salesProductCard,
  uniqueName,
  API_BASE_URL,
} from "./fixtures";

test.describe("Security / RBAC — UI (Cashier session)", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsCashier(page);
  });

  // Each of these routes has its own Admin-only layout.tsx (stock/promotions/reports/
  // sales-history/staff) that checks staff.role and router.replace("/sales") for non-Admins.
  test("/stock redirects to /sales", async ({ page }) => {
    await page.goto("/stock");
    await page.waitForURL((url) => url.pathname === "/sales");
  });

  test("/promotions redirects to /sales", async ({ page }) => {
    await page.goto("/promotions");
    await page.waitForURL((url) => url.pathname === "/sales");
  });

  test("/sales-history redirects to /sales", async ({ page }) => {
    await page.goto("/sales-history");
    await page.waitForURL((url) => url.pathname === "/sales");
  });

  test("/reports redirects to /sales", async ({ page }) => {
    await page.goto("/reports");
    await page.waitForURL((url) => url.pathname === "/sales");
  });

  test("/staff redirects to /sales", async ({ page }) => {
    await page.goto("/staff");
    await page.waitForURL((url) => url.pathname === "/sales");
  });
});

test.describe("Security / RBAC — Navigation menu (003-menu-permission-management)", () => {
  test("Admin sees all 6 menu items, including จัดการสิทธิ์", async ({ page }) => {
    await loginAsAdmin(page);

    const nav = page.locator("nav");
    for (const label of ["ขายสินค้า", "สต็อกสินค้า", "โปรโมชั่น", "ประวัติการขาย", "รายงาน", "จัดการสิทธิ์"]) {
      await expect(nav.getByRole("link", { name: label })).toBeVisible();
    }
  });

  test("Cashier sees only ขายสินค้า in the menu", async ({ page }) => {
    await loginAsCashier(page);

    const nav = page.locator("nav");
    await expect(nav.getByRole("link", { name: "ขายสินค้า" })).toBeVisible();
    for (const label of ["สต็อกสินค้า", "โปรโมชั่น", "ประวัติการขาย", "รายงาน", "จัดการสิทธิ์"]) {
      await expect(nav.getByRole("link", { name: label })).toHaveCount(0);
    }
  });
});

test.describe("Security / RBAC — API (direct requests, no UI)", () => {
  test("Cashier JWT is rejected (403) on every Admin-only endpoint", async ({ request }) => {
    const { token: cashierToken } = await getCashierToken(request);
    const { token: adminToken } = await getAdminToken(request);
    const auth = (t: string) => ({ Authorization: `Bearer ${t}` });

    // Create a real product + order as Admin so the void/export checks below have a
    // legitimate target; the 403 must come from the role check, not a 404.
    const createRes = await request.post(`${API_BASE_URL}/products`, {
      headers: auth(adminToken),
      multipart: { name: uniqueName("rbac-product"), price: "10", quantityOnHand: "5" },
    });
    const product = await createRes.json();

    const checkoutRes = await request.post(`${API_BASE_URL}/sales-orders`, {
      headers: auth(adminToken),
      data: { memberId: null, lines: [{ productId: product.id, quantity: 1 }] },
    });
    const order = await checkoutRes.json();

    const cashierProductCreate = await request.post(`${API_BASE_URL}/products`, {
      headers: auth(cashierToken),
      multipart: { name: uniqueName("rbac-should-fail"), price: "10", quantityOnHand: "5" },
    });
    expect(cashierProductCreate.status()).toBe(403);

    const cashierLowStock = await request.get(`${API_BASE_URL}/products/low-stock`, {
      headers: auth(cashierToken),
    });
    expect(cashierLowStock.status()).toBe(403);

    const cashierPromotionCreate = await request.post(`${API_BASE_URL}/promotions`, {
      headers: auth(cashierToken),
      data: {
        scope: "WholeBill",
        productId: null,
        discountPercent: 10,
        startDate: "2030-01-01",
        endDate: "2030-01-02",
      },
    });
    expect(cashierPromotionCreate.status()).toBe(403);

    const cashierVoid = await request.post(`${API_BASE_URL}/sales-orders/${order.id}/void`, {
      headers: auth(cashierToken),
    });
    expect(cashierVoid.status()).toBe(403);

    const cashierHistorySearch = await request.get(`${API_BASE_URL}/sales-orders?page=1&pageSize=20`, {
      headers: auth(cashierToken),
    });
    expect(cashierHistorySearch.status()).toBe(403);

    const cashierSalesExport = await request.get(`${API_BASE_URL}/sales-orders/export`, {
      headers: auth(cashierToken),
    });
    expect(cashierSalesExport.status()).toBe(403);

    const cashierStockExport = await request.get(`${API_BASE_URL}/reports/stock-levels/export`, {
      headers: auth(cashierToken),
    });
    expect(cashierStockExport.status()).toBe(403);

    const cashierSalesSummary = await request.get(
      `${API_BASE_URL}/reports/sales-summary?granularity=daily&from=2026-01-01&to=2026-12-31`,
      { headers: auth(cashierToken) },
    );
    expect(cashierSalesSummary.status()).toBe(403);

    const cashierStockLevels = await request.get(`${API_BASE_URL}/reports/stock-levels`, {
      headers: auth(cashierToken),
    });
    expect(cashierStockLevels.status()).toBe(403);

    const cashierStaffList = await request.get(`${API_BASE_URL}/staff`, {
      headers: auth(cashierToken),
    });
    expect(cashierStaffList.status()).toBe(403);

    const cashierStaffCreate = await request.post(`${API_BASE_URL}/staff`, {
      headers: auth(cashierToken),
      data: { name: uniqueName("rbac-staff"), username: uniqueName("rbac-user"), password: "password1", role: "Cashier" },
    });
    expect(cashierStaffCreate.status()).toBe(403);
  });

  test("missing or invalid token is rejected (401)", async ({ request }) => {
    const noAuth = await request.get(`${API_BASE_URL}/products/low-stock`);
    expect(noAuth.status()).toBe(401);

    const garbageAuth = await request.get(`${API_BASE_URL}/products/low-stock`, {
      headers: { Authorization: "Bearer not-a-real-token" },
    });
    expect(garbageAuth.status()).toBe(401);
  });

  test("Cashier CAN reach non-admin endpoints (checkout, pricing preview)", async ({
    request,
  }) => {
    const { token: adminToken } = await getAdminToken(request);
    const { token: cashierToken } = await getCashierToken(request);
    const auth = (t: string) => ({ Authorization: `Bearer ${t}` });

    const createRes = await request.post(`${API_BASE_URL}/products`, {
      headers: auth(adminToken),
      multipart: { name: uniqueName("rbac-cashier-ok"), price: "10", quantityOnHand: "5" },
    });
    const product = await createRes.json();

    const preview = await request.get(
      `${API_BASE_URL}/sales-orders/pricing-preview?productId=${product.id}&quantity=1`,
      { headers: auth(cashierToken) },
    );
    expect(preview.status()).toBe(200);

    const checkout = await request.post(`${API_BASE_URL}/sales-orders`, {
      headers: auth(cashierToken),
      data: { memberId: null, lines: [{ productId: product.id, quantity: 1 }] },
    });
    expect(checkout.status()).toBe(201);
  });
});
