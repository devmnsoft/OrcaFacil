import fs from 'node:fs';
const service=fs.readFileSync('src/OrcaFacil.Web/Services/CommercialAutomationService.cs','utf8');
const page=fs.readFileSync('src/OrcaFacil.Web/Pages/CommercialRoutine/Index.cshtml','utf8');
for(const token of ['x.AccountId == AccountId','OrderBy(x => x.NextFollowUpAt ?? x.ValidUntil ?? x.CreatedAt)'])if(!service.includes(token))throw new Error(`rotina: ausente ${token}`);
for(const token of ['of-empty-state','of-filter-bar','Rotina Comercial','LoadError'])if(!page.includes(token))throw new Error(`design comercial: ausente ${token}`);
if((service+page).includes('Math.random'))throw new Error('KPI fake encontrado');
console.log('commercial routine: OK');
