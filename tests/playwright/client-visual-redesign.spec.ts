import { test, expect, type Page } from '@playwright/test';

const baseURL = process.env.ORCA_E2E_BASE_URL;
const pages = [
  ['/Dashboard', 'Visão geral'], ['/Clients', 'Clientes'], ['/Clients/Create', 'Novo cliente'],
  ['/Services', 'Serviços'], ['/Documents', 'Orçamentos'], ['/Documents/CreateBudget', 'Novo orçamento'],
  ['/Documents/CreateReceipt', 'Novo recibo'], ['/Templates', 'Modelos'], ['/Notifications', 'Notificações'],
  ['/Subscription', 'Meu plano'], ['/Support', 'Ajuda'], ['/Discover', 'Conhecer recursos'], ['/Profile', 'emitente'],
] as const;
const viewports = [{ name: 'mobile', width: 375, height: 812 }, { name: 'tablet', width: 768, height: 1024 }, { name: 'desktop', width: 1440, height: 900 }];

async function assertNoOverflow(page: Page) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1)).toBeTruthy();
}

for (const viewport of viewports) {
  test.describe(`cliente ${viewport.name}`, () => {
    test.use({ viewport });
    for (const [route, title] of pages) {
      test(`${route} abre com shell e hierarquia`, async ({ page }, testInfo) => {
        test.skip(!baseURL, 'Requer ambiente autenticado semeado');
        const response = await page.goto(`${baseURL}${route}`);
        expect(response?.status()).toBeLessThan(400);
        await expect(page.locator('main')).toBeVisible();
        await expect(page.locator('body')).toContainText(title, { ignoreCase: true });
        await assertNoOverflow(page);
        await page.screenshot({ path: testInfo.outputPath(`${route.replaceAll('/', '-') || 'home'}-${viewport.name}-after.png`), fullPage: true });
      });
    }
  });
}
