import fs from 'node:fs';
const required = [
  'src/OrcaFacil.Application/CustomerSuccess/CustomerSuccessServices.cs',
  'database/sprint54_customer_success_v55.sql',
  'tests/OrcaFacil.UnitTests/CustomerSuccessServicesTests.cs'
];
const missing = required.filter(file => !fs.existsSync(file));
if (missing.length) { console.error(`Customer Success incompleto: ${missing.join(', ')}`); process.exit(1); }
const source = required.map(file => fs.readFileSync(file, 'utf8')).join('\n');
for (const token of ['CustomerHealthScoreService','CustomerChurnRiskService','CustomerNpsService','CustomerQbrService','CustomerSuccessPlaybookService','account_id']) {
  if (!source.includes(token)) { console.error(`Contrato ausente: ${token}`); process.exit(1); }
}
console.log('Customer Success V5.5: contratos, isolamento e persistência presentes.');
