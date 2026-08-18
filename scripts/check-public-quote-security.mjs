import { readFileSync } from 'node:fs';

const page = readFileSync('src/OrcaFacil.Web/Pages/PublicQuotes/View.cshtml', 'utf8');
const model = readFileSync('src/OrcaFacil.Web/Pages/PublicQuotes/View.cshtml.cs', 'utf8');
const access = readFileSync('src/OrcaFacil.Application/Documents/PublicDocumentAccessContracts.cs', 'utf8');
const failures = [];

const forbidden = [
  ['custo interno', /InternalCost|CostPrice|UnitCost/i],
  ['margem', /Margin(?:Amount|Percent)?/i],
  ['token bruto renderizado', /@(?:Model\.)?(?:Token|PublicToken)\b/i],
  ['informação administrativa', /AuditLog|SystemLog|CreatedByUserId/i]
];
for (const [description, pattern] of forbidden) {
  if (pattern.test(page)) failures.push(`View.cshtml expõe ${description}`);
}

const required = [
  [model, /\[AllowAnonymous\]/, 'página não permite acesso anônimo'],
  [model, /OnPostApproveAsync/, 'handler de aprovação ausente'],
  [model, /OnPostChangeAsync/, 'handler de alteração ausente'],
  [model, /OnPostRejectAsync/, 'handler de recusa ausente'],
  [model, /IdempotencyKey/, 'decisão sem chave de idempotência'],
  [access, /PublicQuoteView/, 'projeção pública dedicada ausente'],
  [page, /data-print-quote/, 'impressão real ausente'],
  [page, /rel="noopener noreferrer"/, 'WhatsApp sem isolamento de navegação']
];
for (const [source, pattern, description] of required) {
  if (!pattern.test(source)) failures.push(description);
}

if (failures.length) {
  console.error(`Segurança da proposta pública reprovada:\n${failures.join('\n')}`);
  process.exit(1);
}
console.log('Proposta pública: projeção segura, acesso anônimo e três decisões reais validados.');
