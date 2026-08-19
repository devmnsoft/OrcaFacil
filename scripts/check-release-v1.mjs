import { spawnSync } from 'node:child_process';

// Keep the release gate self-contained: invoking Node directly avoids shell and npm
// differences between developer workstations and the non-interactive IIS pipeline.
const checks = [
  'check-aspnet-scope',
  'check-build-blockers',
  'check-database-schema',
  'check-js-safety',
  'check-public-navigation',
  'check-authenticated-navigation',
  'check-commercial-flow',
  'check-public-quote-security',
  'check-financial-flow',
  'check-contracts-flow',
  'check-receipts-compliance',
  'check-admin-security',
  'check-open-features',
  'check-design-consistency',
  'check-deploy-readiness'
];

const failures = [];

for (const check of checks) {
  console.log(`\n--- ${check} ---`);
  const result = spawnSync(process.execPath, [`scripts/${check}.mjs`], {
    stdio: 'inherit'
  });

  if (result.error) {
    failures.push(`${check}: ${result.error.message}`);
    continue;
  }

  if (result.status !== 0) {
    failures.push(`${check}: saiu com código ${result.status ?? 'desconhecido'}`);
  }
}

if (failures.length > 0) {
  console.error(`\nRelease Candidate V1 reprovada (${failures.length} check(s)):\n${failures.join('\n')}`);
  process.exit(1);
}

console.log(`\nContrato estático da Release Candidate V1 aprovado em ${checks.length} checks.`);
