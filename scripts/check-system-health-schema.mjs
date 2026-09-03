import { readFileSync } from 'node:fs';
const service=readFileSync('src/OrcaFacil.Persistence/Diagnostics/DatabaseDiagnosticsService.cs','utf8')+readFileSync('src/OrcaFacil.Application/Abstractions/IDatabaseDiagnosticsService.cs','utf8');
const page=readFileSync('src/OrcaFacil.Web/Pages/Diagnostico.cshtml','utf8');
for(const token of ['MissingTable','MissingColumn','IncompatibleType','MissingIndex','ImpactedRoutes','RecommendedPatch','row_version']) if(!service.includes(token)) throw new Error(`SystemHealth sem ${token}`);
if(!page.includes('SchemaDriftIssues') || !page.includes('Patch recomendado')) throw new Error('SystemHealth não apresenta drift acionável');
console.log('system health schema: OK');
