import { test as setup, expect, chromium } from '@playwright/test';

// Autentica o usuário cliente E2E (semado por SQL no banco de teste) e persiste
// o estado da sessão para as specs de experiência do cliente. O onboarding do
// usuário já vem marcado como concluído, então o login deve cair em /Dashboard.
const base = process.env.ORCA_E2E_BASE_URL ?? 'http://localhost:5080';
const email = process.env.ORCAFACIL_E2E_CLIENT_EMAIL ?? 'e2e.cliente@mnsoft.com.br';
const password = process.env.ORCAFACIL_E2E_CLIENT_PASSWORD ?? 'FechôE2E#2026';

setup('autentica a conta cliente E2E e persiste o estado', async () => {
  const browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage();
  await page.goto(`${base}/Auth/Login`);
  await page.getByLabel('CPF, CNPJ ou e-mail').fill(email);
  await page.getByLabel('Senha', { exact: true }).fill(password);
  await page.getByRole('button', { name: /entrar/i }).click();
  await page.waitForURL(/\/(Dashboard|Onboarding)(\/|$)/, { timeout: 25_000 });
  expect(page.url()).toContain('/Dashboard');
  await context.storageState({ path: 'auth/client.json' });
  await browser.close();
});
