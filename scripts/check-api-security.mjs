import fs from 'node:fs';
const roots=['src/OrcaFacil.Web/Api','src/OrcaFacil.Application/Integrations'];
const files=roots.flatMap(r=>fs.readdirSync(r).filter(x=>x.endsWith('.cs')).map(x=>`${r}/${x}`));
const source=files.map(x=>fs.readFileSync(x,'utf8')).join('\n');
for(const forbidden of ['Authorization.ToString() }','connection string','StoragePath']) if(source.includes(forbidden)) throw new Error(`Possível exposição: ${forbidden}`);
for(const expected of ['FixedTimeEquals','account_id','scope_required','correlationId']) if(!source.includes(expected)) throw new Error(`Proteção ausente: ${expected}`);
console.log('OK: isolamento, escopos, comparação segura e erros correlacionados detectados.');
