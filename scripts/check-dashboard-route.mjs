import { readFileSync } from 'node:fs';
const page=readFileSync('src/OrcaFacil.Web/Pages/Dashboard/Index.cshtml','utf8');
const query=readFileSync('src/OrcaFacil.Persistence/Services/CommercialWorkspaceQueryService.cs','utf8');
for(const token of ['of-metrics-empty','metric-card']) if(!page.includes(token)) throw new Error(`Dashboard sem ${token}`);
for(const token of ['AsNoTracking','AccountId']) if(!query.includes(token)) throw new Error(`Dashboard query sem ${token}`);
console.log('dashboard route contract: OK');
