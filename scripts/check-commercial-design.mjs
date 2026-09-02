import fs from 'node:fs';
const view=fs.readFileSync('src/OrcaFacil.Web/Pages/CommercialRoutine/Index.cshtml','utf8');
const css=fs.readFileSync('src/OrcaFacil.Web/wwwroot/css/commercial.css','utf8');
for(const token of ['of-commercial-hero','of-routine-summary','of-filter-bar','of-timeline','of-action-bar'])if(!(view+css).includes(token))throw new Error(`componente ausente: ${token}`);
if(!css.includes('@media'))throw new Error('layout mobile ausente');
console.log('commercial design: OK');

import { runSprint55Check } from './sprint55-design-checks.mjs';
await runSprint55Check('commercial-design');
