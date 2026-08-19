import { existsSync } from 'node:fs';
import { spawnSync } from 'node:child_process';

const requiredArtifacts = [
  'docs/GO-LIVE-V1.md',
  'docs/RELEASE-FINAL-V1.md',
  'docs/HOMOLOGACAO-FINAL-V1.md',
  'docs/REGRESSION-FIXES-V1.md',
  'docs/ROLLBACK-V1.md',
  'docs/OPERACAO-ASSISTIDA-V1.md',
  'RELEASE_CHECKLIST.md',
  'database/backup_postgres.ps1',
  'database/restore_postgres.ps1',
  'scripts/windows/backup-db.ps1',
  'scripts/windows/restore-db.ps1',
  'scripts/windows/update-database.ps1',
  'scripts/windows/publish-release.ps1'
];

const missing = requiredArtifacts.filter(path => !existsSync(path));
if (missing.length > 0) {
  console.error(`Release final incompleta; artefatos ausentes:\n${missing.join('\n')}`);
  process.exit(1);
}

const checks = [
  'check-aspnet-scope',
  'check-build-blockers',
  'check-database-schema',
  'check-razor-pages',
  'check-js-safety',
  'check-public-navigation',
  'check-authenticated-navigation',
  'check-commercial-flow',
  'check-public-quote-security',
  'check-operational-flow',
  'check-financial-flow',
  'check-admin-security',
  'check-security-production',
  'check-appsettings-secrets',
  'check-production-readiness',
  'check-deploy-readiness',
  'check-backup-restore',
  'check-design-consistency',
  'check-accessibility-basics',
  'check-responsive-readiness'
];

const failures = [];
for (const check of checks) {
  console.log(`\n--- ${check} ---`);
  const result = spawnSync(process.execPath, [`scripts/${check}.mjs`], { stdio: 'inherit' });
  if (result.error || result.status !== 0) {
    failures.push(`${check}: ${result.error?.message ?? `código ${result.status ?? 'desconhecido'}`}`);
  }
}

if (failures.length > 0) {
  console.error(`\nRelease final reprovada (${failures.length}):\n${failures.join('\n')}`);
  process.exit(1);
}

console.log(`\nRelease final aprovada em ${checks.length} gates estáticos.`);
