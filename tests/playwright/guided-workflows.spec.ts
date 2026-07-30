import { test, expect } from '@playwright/test';
const baseURL = process.env.ORCA_E2E_BASE_URL;
test.beforeEach(async ({ page }) => { test.skip(!baseURL, 'Requer conta autenticada semeada'); await page.goto(`${baseURL}/Dashboard`); });
test('sidebar recolhe, preserva o item ativo e salva preferência', async ({ page }) => {
  await page.getByRole('button', { name: 'Recolher menu' }).click();
  await expect(page.locator('[data-client-shell]')).toHaveClass(/is-collapsed/);
  expect(await page.evaluate(() => localStorage.getItem('of-sidebar-collapsed'))).toBe('true');
  await expect(page.locator('.of-grouped-menu [aria-current="page"]')).toBeVisible();
});
test('command palette abre por teclado e fecha com Escape', async ({ page }) => {
  await page.keyboard.press('Control+k');
  await expect(page.getByRole('dialog', { name: 'Busca global' })).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(page.locator('[data-search-dialog]')).toBeHidden();
});
test('navegação mobile abre folha de criação', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 812 });
  await page.getByRole('button', { name: 'Novo' }).click();
  await expect(page.getByRole('dialog', { name: /O que você quer criar/i })).toBeVisible();
  await expect(page.getByText('Prepare uma proposta com serviços')).toBeVisible();
});
