import { test, expect } from '@playwright/test';
const baseURL = process.env.ORCA_E2E_BASE_URL;
test('cada demonstração carrega conteúdo e imagem próprios', async ({ page }) => {
  test.skip(!baseURL, 'Requer conta autenticada semeada');
  await page.goto(`${baseURL}/Discover`);
  const buttons = page.getByRole('button', { name: 'Testar demonstração' });
  expect(await buttons.count()).toBe(9);
  await buttons.nth(2).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toContainText('Aprovação pelo cliente');
  await expect(dialog.getByText('nenhum dado real será alterado')).toBeVisible();
});
test('ajuda contextual abre como drawer e devolve foco', async ({ page }) => {
  test.skip(!baseURL, 'Requer conta autenticada semeada');
  await page.goto(`${baseURL}/Dashboard`);
  const trigger = page.getByRole('button', { name: 'Como usar esta página' });
  await trigger.click();
  await expect(page.getByRole('dialog', { name: /Transforme a informação/i })).toBeVisible();
  await page.getByRole('button', { name: 'Fechar ajuda' }).click();
  await expect(trigger).toBeFocused();
});
