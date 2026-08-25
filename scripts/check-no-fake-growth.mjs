import fs from 'node:fs';
const files = ['src/OrcaFacil.Application/Growth/GrowthRules.cs','database/sprint33_growth_v34.sql'];
const forbidden = [/Math\.random\s*\(/, /checkout fake/i, /pagamento.*aprovado.*sem confirmação/i];
const failures = files.flatMap(file => forbidden.filter(rule => rule.test(fs.readFileSync(file, 'utf8'))).map(rule => `${file}: ${rule}`));
if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log('[no-fake-growth] OK: regras determinísticas e receita vinculada a pagamento.');
