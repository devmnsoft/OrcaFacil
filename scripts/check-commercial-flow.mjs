import { existsSync, readFileSync } from 'node:fs';

const failures = [];
const requiredPages = [
  'Clients/Create', 'Clients/Details', 'Services/Create', 'Documents/CreateBudget',
  'Documents/Details', 'PublicQuotes/View', 'WorkOrders/Details', 'Schedule/Index',
  'Payments/Register', 'Receipts/Create', 'Receipts/Details', 'Alerts/Index',
  'Reports/CommercialFunnel', 'Dashboard/Index'
];

for (const page of requiredPages) {
  for (const extension of ['cshtml', 'cshtml.cs']) {
    const file = `src/OrcaFacil.Web/Pages/${page}.${extension}`;
    if (!existsSync(file)) failures.push(`${file}: rota comercial ausente`);
  }
}

const contracts = [
  ['src/OrcaFacil.Web/Pages/Clients/Details.cshtml', /asp-page="\/Documents\/(?:New|CreateBudget)"/i, 'Cliente 360 sem ação real de orçamento'],
  ['src/OrcaFacil.Web/Pages/Documents/Details.cshtml.cs', /OnPost(?:Generate)?PublicLinkAsync/i, 'orçamento sem geração real de link público'],
  ['src/OrcaFacil.Web/Pages/WorkOrders/Details.cshtml', /asp-page="\/Payments\/Register"/i, 'OS sem ação real de pagamento'],
  ['src/OrcaFacil.Web/Pages/Payments/Register.cshtml.cs', /RedirectToPage\("\/Receipts\/Create"/i, 'pagamento não encaminha para emissão de recibo'],
  ['src/OrcaFacil.Web/Pages/Dashboard/Index.cshtml', /asp-page="\/Documents\/(?:New|CreateBudget)"/i, 'dashboard sem atalho real de orçamento']
];

for (const [file, pattern, message] of contracts) {
  if (existsSync(file) && !pattern.test(readFileSync(file, 'utf8'))) failures.push(`${file}: ${message}`);
}

if (failures.length) {
  console.error(`Fluxo comercial incompleto:\n${failures.join('\n')}`);
  process.exit(1);
}
console.log(`Fluxo comercial validado: ${requiredPages.length} etapas e ${contracts.length} transições reais.`);
