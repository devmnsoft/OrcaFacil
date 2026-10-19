import { defineConfig, devices } from '@playwright/test';

// As specs esperam ORCA_E2E_BASE_URL (cliente) e ORCA_ADMIN_E2E_BASE_URL (SuperAdmin).
// Quando ausentes, todas se auto-pulam ("Requer ambiente semeado"), então este config
// só produz evidência quando há um servidor Web rodando contra um banco semeado.
const base = process.env.ORCA_E2E_BASE_URL ?? 'http://localhost:5080';

export default defineConfig({
  testDir: '.',
  timeout: 30_000,
  expect: { timeout: 5_000 },
  fullyParallel: true,
  workers: 2,
  reporter: [['list']],
  outputDir: 'test-results',
  use: {
    browserName: 'chromium',
    headless: true,
    // Navegador real de usuário brasileiro: a app negocia o idioma pela
    // Accept-Language e formata datas/moeda por fuso semântico.
    locale: 'pt-BR',
    timezoneId: 'America/Sao_Paulo',
    viewport: { width: 1366, height: 900 },
    trace: 'retain-on-failure',
  },
  projects: [
    { name: 'setup-admin', testMatch: /setup\.admin\.ts/ },
    { name: 'setup-client', testMatch: /setup\.client\.ts/ },
    {
      name: 'admin',
      testMatch: /admin-visual-redesign\.spec\.ts/,
      dependencies: ['setup-admin'],
      use: { ...devices['Desktop Chrome'], storageState: 'auth/admin.json', env: { ORCA_ADMIN_E2E_BASE_URL: base } },
    },
    {
      name: 'client',
      testMatch: /(client-visual-redesign|demos-and-help|experience-system|guided-workflows|message-drafts|commercial-review)\.spec\.ts/,
      dependencies: ['setup-client'],
      use: { ...devices['Desktop Chrome'], storageState: 'auth/client.json', env: { ORCA_E2E_BASE_URL: base } },
    },
  ],
});
