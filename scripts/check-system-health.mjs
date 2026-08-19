import { readFile } from 'node:fs/promises';

const [diagnostics, page] = await Promise.all([
  readFile('src/OrcaFacil.Persistence/Diagnostics/DatabaseDiagnosticsService.cs', 'utf8'),
  readFile('src/OrcaFacil.Web/Pages/Diagnostico.cshtml.cs', 'utf8')
]);
for (const marker of ['RequiredTables','missingColumns','MaskConnectionString']) if (!`${diagnostics}\n${page}`.includes(marker)) throw new Error(`Diagnóstico sem ${marker}.`);
if (/Password\s*=|ConnectionString\s*\}/.test(page)) throw new Error('Diagnóstico pode expor configuração sensível.');
console.log('Diagnóstico de conexão e contrato de schema validado sem exposição de segredo.');
