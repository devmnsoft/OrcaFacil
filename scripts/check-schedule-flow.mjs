import { readFileSync } from 'node:fs';
const page=readFileSync('src/OrcaFacil.Web/Pages/Schedule/Index.cshtml','utf8'); const model=readFileSync('src/OrcaFacil.Web/Pages/Schedule/Index.cshtml.cs','utf8');
const failures=[]; for(const view of ['today','week','overdue','unscheduled']) if(!page.includes(`view == \"${view}\"`) && !page.includes(`View == \"${view}\"`)) failures.push(`visão ${view} ausente`);
if(!model.includes('AccountId == account.AccountId')) failures.push('agenda sem isolamento de conta'); if(!model.includes('AsNoTracking')) failures.push('agenda sem leitura otimizada');
if(failures.length){console.error(failures.join('\n'));process.exit(1)} console.log('Agenda operacional validada.');
