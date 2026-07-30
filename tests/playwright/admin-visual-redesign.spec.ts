import { test, expect } from '@playwright/test';
const baseURL = process.env.ORCA_ADMIN_E2E_BASE_URL ?? process.env.ORCA_E2E_BASE_URL;
const routes = ['/Admin/Dashboard', '/Admin/Users', '/Admin/Plans', '/Admin/Payments', '/Admin/Settings/Database'];
for (const viewport of [{ name: 'mobile', width: 375, height: 812 }, { name: 'tablet', width: 768, height: 1024 }, { name: 'desktop', width: 1440, height: 900 }]) {
  test.describe(`admin ${viewport.name}`, () => {
    test.use({ viewport });
    for (const route of routes) test(`${route} mantém central operacional`, async ({ page }, testInfo) => {
      test.skip(!baseURL, 'Requer SuperAdministrador autenticado');
      const response = await page.goto(`${baseURL}${route}`);
      expect(response?.status()).toBeLessThan(400);
      await expect(page.locator('#admin-content')).toBeVisible();
      if (viewport.width >= 1000) await expect(page.getByRole('navigation', { name: /Administração da plataforma/i })).toBeVisible();
      await page.screenshot({ path: testInfo.outputPath(`${route.replaceAll('/', '-')}-${viewport.name}-after.png`), fullPage: true });
    });
  });
}
