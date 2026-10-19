import { test, expect } from '@playwright/test';

const baseURL = process.env.ORCA_E2E_BASE_URL ?? 'http://localhost:5080';
// Evidência do E-3: ajuda contextual com pergunta livre (RAG sobre guias/artigos publicados).
test.skip(!process.env.ORCA_E2E_BASE_URL, 'Requer ambiente semeado');

test('ajuda contextual responde pergunta sobre a tela com fontes e aviso honesto', async ({ page }) => {
  await page.goto(`${baseURL}/Dashboard`);
  await page.getByRole('button', { name: 'Como usar esta página' }).click();

  const drawer = page.locator('[data-help-drawer]');
  await expect(drawer).toBeVisible();
  await expect(drawer.locator('[data-help-title]')).toContainText(/Visão geral/i);
  await expect(drawer.getByRole('heading', { name: 'Pergunte sobre esta tela' })).toBeVisible();

  const question = drawer.locator('[data-help-ask-question]');
  await expect(question).toBeVisible();
  await question.fill('Como acompanhar as propostas aguardando resposta?');
  await drawer.locator('[data-help-ask-submit]').click();

  const answer = drawer.locator('[data-help-ask-answer]');
  await expect(answer).toHaveAttribute('aria-busy', 'false', { timeout: 15000 });
  await expect(answer).toContainText(/propostas|Visão geral/i);
  await expect(answer).toContainText(/Nenhuma ação foi executada|apoio de IA/i);
  await expect(answer).toContainText(/Fontes consultadas/i);
});

test('sem artigo publicado a resposta sai dos guias oficiais e diz que nada foi executado', async ({ page }) => {
  // A base de conhecimento está vazia neste ambiente; o corpus é formado pelos guias
  // estáticos do produto. A resposta precisa vir deles e nunca alegar ação executada.
  await page.goto(`${baseURL}/Documents/Index`);
  await page.getByRole('button', { name: 'Como usar esta página' }).click();
  const drawer = page.locator('[data-help-drawer]');
  await expect(drawer).toBeVisible();

  await drawer.locator('[data-help-ask-question]').fill('Como acompanhar as propostas aguardando resposta?');
  await drawer.locator('[data-help-ask-submit]').click();

  const answer = drawer.locator('[data-help-ask-answer]');
  await expect(answer).toHaveAttribute('aria-busy', 'false', { timeout: 15000 });
  await expect(answer).toContainText(/propostas aguardando resposta/i);
  await expect(answer).toContainText(/Fontes consultadas/i);
  await expect(answer).toContainText(/Nenhuma ação foi executada/i);
});

test('pergunta curta é recusada sem chamar o servidor', async ({ page }) => {
  await page.goto(`${baseURL}/Dashboard`);
  await page.getByRole('button', { name: 'Como usar esta página' }).click();
  const drawer = page.locator('[data-help-drawer]');
  await expect(drawer).toBeVisible();

  await drawer.locator('[data-help-ask-question]').fill('oi');
  await drawer.locator('[data-help-ask-submit]').click();
  await expect(drawer.locator('[data-help-ask-answer]')).toContainText('pelo menos 3 caracteres');
});
