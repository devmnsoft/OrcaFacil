import { access, readFile } from 'node:fs/promises';
import { join } from 'node:path';

const webRoot = 'src/OrcaFacil.Web';
const failures = [];

// These are executable contracts for the V1 journeys.  Keeping the contract here
// makes a removed/renamed Razor Page fail release validation instead of becoming a
// dead menu item discovered by a customer.
const personas = {
  'Visitante público': [
    'Pages/Index.cshtml', 'Pages/Precos.cshtml', 'Pages/Support/Index.cshtml',
    'Pages/Auth/Login.cshtml', 'Pages/Auth/Register.cshtml', 'Pages/Termos.cshtml',
    'Pages/Privacidade.cshtml'
  ],
  'Novo usuário': [
    'Pages/Auth/Register.cshtml', 'Pages/Auth/Login.cshtml', 'Pages/Onboarding/Index.cshtml',
    'Pages/Onboarding/Business.cshtml', 'Pages/Onboarding/Client.cshtml',
    'Pages/Onboarding/Service.cshtml', 'Pages/Onboarding/Budget.cshtml',
    'Pages/Dashboard/Index.cshtml'
  ],
  Comercial: [
    'Pages/Clients/Index.cshtml', 'Pages/Clients/Details.cshtml', 'Pages/Services/Index.cshtml',
    'Pages/Documents/CreateBudget.cshtml', 'Pages/Documents/Details.cshtml',
    'Pages/CommercialRoutine/Index.cshtml', 'Pages/MessageTemplates/Index.cshtml',
    'Pages/Alerts/Index.cshtml'
  ],
  'Cliente externo': ['Pages/PublicQuotes/View.cshtml'],
  Operacional: [
    'Pages/WorkOrders/Index.cshtml', 'Pages/WorkOrders/Details.cshtml',
    'Pages/Schedule/Index.cshtml', 'Pages/Clients/Details.cshtml'
  ],
  Financeiro: [
    'Pages/Payments/Index.cshtml', 'Pages/Payments/Register.cshtml',
    'Pages/Payments/Details.cshtml', 'Pages/Receipts/Index.cshtml',
    'Pages/Receipts/Details.cshtml', 'Pages/Reports/Financial.cshtml'
  ],
  'Administrador da conta': [
    'Pages/Settings/Index.cshtml', 'Pages/Settings/Company.cshtml',
    'Pages/Settings/Branding.cshtml', 'Pages/Settings/Documents.cshtml',
    'Pages/Settings/Payments.cshtml', 'Pages/Settings/Users.cshtml',
    'Pages/Settings/Permissions.cshtml', 'Pages/Settings/Notifications.cshtml',
    'Pages/Settings/Security.cshtml', 'Pages/Subscription/Index.cshtml'
  ],
  SuperAdmin: [
    'Areas/Admin/Pages/Dashboard.cshtml', 'Areas/Admin/Pages/Accounts/Details.cshtml',
    'Areas/Admin/Pages/Users/Index.cshtml', 'Areas/Admin/Pages/Plans/Index.cshtml',
    'Areas/Admin/Pages/Logs.cshtml', 'Areas/Admin/Pages/Audit/Index.cshtml',
    'Areas/Admin/Pages/Settings/Database.cshtml', 'Areas/Admin/Pages/EmailOutbox/Index.cshtml'
  ]
};

for (const [persona, pages] of Object.entries(personas)) {
  for (const relative of pages) {
    try { await access(join(webRoot, relative)); }
    catch { failures.push(`${persona}: página obrigatória ausente (${relative})`); }
  }
}

const contracts = [
  ['proposta: aprovar', 'Pages/PublicQuotes/View.cshtml.cs', /OnPostApproveAsync/],
  ['proposta: recusar', 'Pages/PublicQuotes/View.cshtml.cs', /OnPostRejectAsync/],
  ['proposta: solicitar alteração', 'Pages/PublicQuotes/View.cshtml.cs', /OnPostChangeAsync/],
  ['OS: iniciar execução', 'Pages/WorkOrders/Details.cshtml.cs', /OnPostStartAsync/],
  ['OS: concluir', 'Pages/WorkOrders/Details.cshtml.cs', /OnPostCompleteAsync/],
  ['financeiro: registrar', 'Pages/Payments/Register.cshtml.cs', /OnPostAsync/],
  ['financeiro: valor positivo', 'Pages/Payments/Register.cshtml.cs', /Range\(typeof\(decimal\),\s*"0\.01"/],
  ['financeiro: reverter', 'Pages/Payments/Details.cshtml.cs', /OnPostReverseAsync/],
  ['recibo: aviso fiscal', 'Pages/Receipts/Details.cshtml', /FiscalNotice/],
  ['isolamento da OS', 'Pages/WorkOrders/Details.cshtml.cs', /x\.AccountId\s*==\s*account\.AccountId/]
];

for (const [label, relative, pattern] of contracts) {
  try {
    const source = await readFile(join(webRoot, relative), 'utf8');
    if (!pattern.test(source)) failures.push(`${label}: contrato não encontrado em ${relative}`);
  } catch { failures.push(`${label}: arquivo não encontrado (${relative})`); }
}

if (failures.length) {
  console.error(`Falha nos fluxos por perfil:\n- ${failures.join('\n- ')}`);
  process.exit(1);
}

const pageCount = Object.values(personas).reduce((total, pages) => total + pages.length, 0);
console.log(`Fluxos de ${Object.keys(personas).length} perfis validados (${pageCount} contratos de página e ${contracts.length} contratos funcionais).`);
