import { existsSync, readFileSync } from 'node:fs';
const required = ['database/script_completop.sql','database/patch_release_candidate_schema.sql','DEPLOY-IIS.md','BACKUP.md','ENVIRONMENTS.md','src/OrcaFacil.Web/web.config','scripts/windows/start-local.ps1','scripts/windows/stop-local.ps1'];
const missing = required.filter(x => !existsSync(x));
if (missing.length) throw new Error(`Artefatos de produção ausentes: ${missing.join(', ')}`);
const program = readFileSync('src/OrcaFacil.Web/Program.cs','utf8');
for (const marker of ['PersistKeysToFileSystem','UseHsts','UseHttpsRedirection','MaintenanceModeMiddleware','MapHealthChecks("/health"']) if (!program.includes(marker)) throw new Error(`Program.cs sem ${marker}`);
console.log('Produção: persistência de chaves, HTTPS, manutenção, health e artefatos operacionais OK.');
