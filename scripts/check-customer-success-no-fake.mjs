import fs from 'node:fs';
import path from 'node:path';
const roots = ['src/OrcaFacil.Application/CustomerSuccess','tests/OrcaFacil.UnitTests/CustomerSuccessServicesTests.cs'];
const files = roots.flatMap(root => fs.statSync(root).isDirectory() ? fs.readdirSync(root).map(x => path.join(root,x)) : [root]);
const forbidden = [/Math\.random/i, /new\s+Random\s*\(/, /NotImplementedException/, /\b(fake|mock)\b/i];
const failures=[];
for(const file of files){ const text=fs.readFileSync(file,'utf8'); for(const rule of forbidden) if(rule.test(text)) failures.push(`${file}: ${rule}`); }
if(failures.length){ console.error(failures.join('\n')); process.exit(1); }
console.log('Customer Success: nenhum score, NPS, churn, QBR ou playbook inventado.');
