import { test, expect } from '@playwright/test';

const baseURL = process.env.ORCA_E2E_BASE_URL ?? 'http://localhost:5080';
// Evidência da Sprint E: rascunho de mensagem IA no acompanhamento comercial.
test.skip(!process.env.ORCA_E2E_BASE_URL, 'Requer ambiente semeado');

test('detalhes do orçamento gera rascunho de WhatsApp para follow-up', async ({ page }) => {
  await page.goto(`${baseURL}/Documents/Index`);
  await page.getByRole('link', { name: 'Detalhes' }).first().click();
  await expect(page.locator('#follow-up-title')).toBeVisible();

  await page.getByRole('button', { name: 'Rascunho para WhatsApp' }).click();
  const draft = page.locator('[data-share-center] [data-share-message]');
  await expect(draft).toBeVisible();
  await expect(draft).toHaveValue(/proposta/i);
  await expect(page.locator('[data-share-center] [data-copy-message]')).toBeVisible();
  await expect(page.locator('[data-share-center] [data-share-whatsapp]')).toBeVisible();
  await expect(page.locator('[data-share-center] small')).toContainText('NUNCA');
});

test('rascunho de e-mail usa tom formal e menciona o número da proposta', async ({ page }) => {
  await page.goto(`${baseURL}/Documents/Index`);
  await page.getByRole('link', { name: 'Detalhes' }).first().click();
  await expect(page.locator('#follow-up-title')).toBeVisible();

  await page.getByRole('button', { name: 'Rascunho para e-mail' }).click();
  const draft = page.locator('[data-share-center] [data-share-message]');
  await expect(draft).toBeVisible();
  await expect(draft).toHaveValue(/proposta comercial/i);
  await expect(page.locator('[data-share-center] span')).toContainText('Email');
});
