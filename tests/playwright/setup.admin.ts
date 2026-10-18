import { test as setup, expect, chromium } from '@playwright/test';

// Autentica o SuperAdministrador semeado pelo SuperAdminSeeder
// (ORCAFACIL_ALLOW_LOCAL_SUPERADMIN_SEED=true) e persiste o estado da sessão
// para as specs administrativas (/Admin/*).
const base = process.env.ORCA_E2E_BASE_URL ?? 'http://localhost:5080';
const email = process.env.ORCAFACIL_SUPERADMIN_EMAIL ?? 'superadmin@mnsoft.com.br';
const password = process.env.ORCAFACIL_SUPERADMIN_PASSWORD ?? 'OrcaFacil@2026!Trocar';

setup('autentica o SuperAdministrador e persiste o estado', async () => {
  const browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage();
  await page.goto(`${base}/Auth/Login`);
  await page.getByLabel('CPF, CNPJ ou e-mail').fill(email);
  await page.getByLabel('Senha', { exact: true }).fill(password);
  await page.getByRole('button', { name: /entrar/i }).click();
  // Com conta limpa o SuperAdmin cai em /SuperAdmin (Index implícito); tolera mudança de senha.
  await page.waitForURL(/\/(Auth\/ChangePassword|SuperAdmin)/, { timeout: 25_000 });
  if (page.url().includes('/Auth/ChangePassword')) {
    const newPass = `${password}!novo`;
    for (const label of ['Nova senha', 'Confirmar nova senha']) {
      await page.getByLabel(label).fill(newPass);
    }
    await page.getByRole('button', { name: /salvar|atualizar|alterar/i }).click();
  }
  await page.waitForURL(/\/SuperAdmin/, { timeout: 25_000 });
  expect(page.url()).toContain('/SuperAdmin');
  await context.storageState({ path: 'auth/admin.json' });
  await browser.close();
});
