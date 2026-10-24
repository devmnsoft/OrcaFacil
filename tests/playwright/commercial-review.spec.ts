import { test, expect } from '@playwright/test';

const baseURL = process.env.ORCA_E2E_BASE_URL ?? 'http://localhost:5080';
const draftId = process.env.ORCA_E2E_DRAFT_ID ?? 'a0e20000-0000-4000-8000-000000000102';
// Evidência do E-2: assistente de revisão comercial em orçamento em rascunho.
test.skip(!process.env.ORCA_E2E_BASE_URL, 'Requer ambiente semeado');

const openAiSection = async (page: import('@playwright/test').Page) => {
  await page.goto(`${baseURL}/Documents/CreateBudget?id=${draftId}`);
  // Wizard inicia no passo 1 (cliente); avança até "Condições" (índice 2), onde vive o assistente.
  await expect(page.locator('[data-budget-wizard]')).toBeVisible();
  const chips = page.locator('[data-step-target]');
  await chips.nth(2).click(); // 1º: 0→1 (exige cliente), depois 2: 1→2 (exige item com descrição)
  await page.waitForTimeout(400); // autosave (debounce) após o go()
  await chips.nth(2).click();
  await page.waitForTimeout(400);
  // Expande a seção "Adicionar garantia, escopo e condições" (details colapsado).
  const details = page.locator('details.of-progressive-fields').filter({ hasText: 'Assistente IA' });
  await details.locator('summary').click();
  await expect(page.locator('#btn-ai-assist')).toBeVisible();
};

test('assistente IA revisa escopo do rascunho e mostra pontos de atenção com aviso honesto', async ({ page }) => {
  await openAiSection(page);
  await page.locator('#btn-ai-assist').click();

  const preview = page.locator('#ai-assist-preview');
  await expect(preview).toBeVisible();

  const findings = page.locator('#ai-findings-container');
  await expect(findings).toContainText(/Pontos de atenção|Nenhum dado faltante/i);
  await expect(findings).toContainText(/regras de consistência/i);

  const improved = page.locator('#ai-improved-notes');
  await expect(improved).toHaveValue(/Escopo/i);
});

test('aplicar sugestão leva o texto do escopo ao campo de notas e descartar fecha', async ({ page }) => {
  await openAiSection(page);
  await page.locator('#btn-ai-assist').click();
  const preview = page.locator('#ai-assist-preview');
  await expect(preview).toBeVisible();
  const scope = await page.locator('#ai-improved-notes').inputValue();
  await expect(scope).not.toBe('');

  await page.locator('#btn-ai-accept').click();
  await expect(page.locator('textarea[data-field="notes"]')).toHaveValue(scope);

  // Nova análise e descarte fecha o painel
  await page.locator('#btn-ai-assist').click();
  await expect(preview).toBeVisible();
  await page.locator('#btn-ai-discard').click();
  await expect(preview).not.toBeVisible();
});
