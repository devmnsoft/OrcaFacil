import { existsSync, readFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';

const required = [
  'database/script_completop.sql', 'database/patch_release_candidate_schema.sql',
  'database/backup_postgres.ps1', 'database/restore_postgres.ps1',
  'scripts/setup-local.ps1', 'scripts/update-database-local.ps1', 'scripts/publish-iis.ps1',
  'src/OrcaFacil.Web/appsettings.Production.json'
];
const missing = required.filter(path => !existsSync(path));
if (missing.length) { console.error(`Arquivos RC ausentes:\n${missing.join('\n')}`); process.exit(1); }

const production = JSON.parse(readFileSync('src/OrcaFacil.Web/appsettings.Production.json', 'utf8'));
const serialized = JSON.stringify(production);
if (/Password\s*=|Host=.*;|postgres:\/\//i.test(serialized)) {
  console.error('appsettings.Production.json parece conter uma connection string ou senha versionada.');
  process.exit(1);
}
for (const check of ['check:js-safety', 'check:public-navigation', 'check:authenticated-navigation', 'check:routes', 'check:razor-pages']) {
  const result = spawnSync(process.platform === 'win32' ? 'npm.cmd' : 'npm', ['run', check], { stdio: 'inherit' });
  if (result.status !== 0) process.exit(result.status ?? 1);
}
console.log('Release readiness: artefatos operacionais, configuração e verificações estáticas aprovados.');
