import { spawnSync } from 'node:child_process';

// Keep the final gate explicit and deterministic. Build/test/publish remain separate
// commands because this check is also useful on hosts that only run the static QA job.
const checks = [
  'check:build-blockers',
  'check:database-schema',
  'check:razor-pages',
  'check:js-safety',
  'check:public-navigation',
  'check:authenticated-navigation',
  'check:commercial-flow',
  'check:operational-flow',
  'check:financial-flow',
  'check:admin-security',
  'check:production-readiness',
  'check:deploy-readiness',
  'check:appsettings-secrets',
  'check:design-consistency',
  'check:accessibility-basics'
];

for (const check of checks) {
  const command = process.platform === 'win32' ? 'npm.cmd' : 'npm';
  const result = spawnSync(command, ['run', check], { stdio: 'inherit' });
  if (result.error) {
    console.error(`Falha ao iniciar ${check}: ${result.error.message}`);
    process.exit(1);
  }
  if (result.status !== 0) {
    console.error(`Gate final interrompido em ${check}.`);
    process.exit(result.status ?? 1);
  }
}

console.log(`Release final: ${checks.length} verificações obrigatórias aprovadas.`);
