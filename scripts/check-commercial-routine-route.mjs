import { readFileSync } from 'node:fs';
const page=readFileSync('src/OrcaFacil.Web/Pages/CommercialRoutine/Index.cshtml','utf8');
const service=readFileSync('src/OrcaFacil.Web/Services/CommercialAutomationService.cs','utf8');
if(!page.includes('of-empty-state')) throw new Error('CommercialRoutine sem empty state');
if(!service.includes('AccountId')) throw new Error('CommercialRoutine sem isolamento de conta');
for(const source of [page,service]) if(source.includes('Math.random')) throw new Error('CommercialRoutine contém dado aleatório');
console.log('commercial routine route contract: OK');
