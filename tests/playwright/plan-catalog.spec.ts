import { test, expect } from '@playwright/test';

const baseURL = process.env.ORCA_E2E_BASE_URL ?? 'http://localhost:5080';
// Evidência da Sprint P (catálogo de planos): a matriz de recursos agora é provisionada,
// então a página pública de preços exibe os diferenciais vendidos e continua usável no celular.
test.skip(!process.env.ORCA_E2E_BASE_URL, 'Requer ambiente semeado');

const diferenciais = ['Aprovação pública', 'Ordens de serviço', 'Pagamentos manuais'];

test('página de preços exibe os diferenciais dos planos pagos', async ({ page }) => {
  await page.goto(`${baseURL}/Precos`);
  await expect(page.getByRole('heading', { name: /Capacidade para cada fase/i })).toBeVisible();

  for (const nome of diferenciais) {
    await expect(page.getByText(nome, { exact: false }).first()).toBeVisible();
  }

  await expect(page.getByRole('heading', { name: 'Profissional', exact: true })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Negócio', exact: true })).toBeVisible();
});

test('página de preços é usável no celular sem rolagem horizontal', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto(`${baseURL}/Precos`);

  const cards = page.locator('.of-price-card');
  await expect(cards).toHaveCount(3);
  await expect(cards.first().getByRole('link', { name: /Testar 15 dias/i })).toBeVisible();

  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow).toBeLessThanOrEqual(1);
});
