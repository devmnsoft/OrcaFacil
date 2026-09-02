import { readFileSync } from 'node:fs';
const page=readFileSync('src/OrcaFacil.Web/Pages/Documents/New.cshtml','utf8');
const service=readFileSync('src/OrcaFacil.Persistence/Services/GuidedBudgetStartService.cs','utf8');
if(!page.includes('of-start-empty') || !page.includes('of-start-options')) throw new Error('Documents/New sem wizard ou empty state');
if(!service.includes('AccountId') || !service.includes('IsActive')) throw new Error('Seleção de clientes sem isolamento/atividade');
console.log('documents/new route contract: OK');
