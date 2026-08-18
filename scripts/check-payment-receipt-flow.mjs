import { readFileSync } from 'node:fs';

const paymentModel = readFileSync('src/OrcaFacil.Web/Pages/Payments/Register.cshtml.cs', 'utf8');
const paymentPage = readFileSync('src/OrcaFacil.Web/Pages/Payments/Register.cshtml', 'utf8');
const receiptCreate = readFileSync('src/OrcaFacil.Web/Pages/Receipts/Create.cshtml.cs', 'utf8');
const receiptDetails = readFileSync('src/OrcaFacil.Web/Pages/Receipts/Details.cshtml', 'utf8');
const failures = [];

const requirements = [
  [paymentModel, /\[Range\(typeof\(decimal\),\s*"0\.01"/, 'pagamento não exige valor positivo'],
  [paymentModel, /x\.AccountId\s*==\s*account\.AccountId/, 'origem do pagamento não está isolada por conta'],
  [paymentModel, /IdempotencyKey/, 'pagamento sem proteção contra reenvio'],
  [paymentPage, /asp-validation-for="Input\.Amount"/, 'erro de valor não aparece no formulário'],
  [paymentModel, /RedirectToPage\("\/Receipts\/Create"/, 'pagamento não abre emissão do recibo'],
  [receiptCreate, /paymentId/i, 'recibo não aceita origem de pagamento'],
  [receiptDetails, /FiscalNotice/, 'recibo não exibe aviso fiscal persistido'],
  [receiptDetails, /asp-page-handler="Cancel"/, 'recibo não oferece cancelamento real'],
  [receiptDetails, /CancellationReason/, 'cancelamento do recibo não exige motivo']
];
for (const [source, pattern, description] of requirements) {
  if (!pattern.test(source)) failures.push(description);
}

if (failures.length) {
  console.error(`Fluxo pagamento/recibo incompleto:\n${failures.join('\n')}`);
  process.exit(1);
}
console.log(`Pagamento e recibo validados em ${requirements.length} contratos de domínio e interface.`);
