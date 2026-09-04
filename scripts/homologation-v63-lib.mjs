import { existsSync, readFileSync } from 'node:fs';

const read = path => {
  if (!existsSync(path)) throw new Error(`arquivo obrigatório ausente: ${path}`);
  return readFileSync(path, 'utf8');
};

const requireAll = (path, values) => {
  const source = read(path).toLowerCase();
  for (const value of values) {
    if (!source.includes(value.toLowerCase())) throw new Error(`${path}: contrato ausente: ${value}`);
  }
};

const page = name => `src/OrcaFacil.Web/Pages/${name}`;

export function checkV63(mode = 'all') {
  const checks = {
    routes() {
      for (const route of [
        'Index.cshtml', 'Auth/Login.cshtml', 'Auth/Register.cshtml', 'Auth/ForgotPassword.cshtml',
        'Auth/ResetPassword.cshtml', 'Onboarding/Index.cshtml', 'Dashboard/Index.cshtml',
        'Clients/Index.cshtml', 'Clients/Create.cshtml', 'Clients/Details.cshtml',
        'Documents/Index.cshtml', 'Documents/New.cshtml', 'CommercialRoutine/Index.cshtml',
        'Diagnostico.cshtml', 'Admin/Index.cshtml', 'Admin/QualityGate.cshtml'
      ]) read(page(route));
      requireAll('src/OrcaFacil.Application/Quality/QualityGateService.cs', ['CriticalRoutes', 'Portal/Index', 'PartnerPortal/Index']);
    },
    permissions() {
      requireAll('src/OrcaFacil.Application/Security/PermissionCodes.cs', [
        'DashboardView', 'ClientsView', 'DocumentsView', 'WorkOrdersView', 'PaymentsView',
        'CustomerSuccessView', 'PortalPaymentsView', 'PartnerPortalCommissionsView', 'SensitiveDataView'
      ]);
      requireAll(page('Documents/New.cshtml.cs'), ['[Authorize', 'IGuidedBudgetStartService']);
      for (const model of ['WorkOrders/Details.cshtml.cs', 'Payments/Index.cshtml.cs'])
        requireAll(page(model), ['[Authorize', 'AccountId']);
      requireAll(page('Admin/QualityGate.cshtml.cs'), ['Authorize(Policy', 'SuperAdminOnly']);
    },
    commercial() {
      requireAll(page('Documents/New.cshtml'), ['of-start-options', 'of-start-empty', 'data-picker-search']);
      requireAll(page('Documents/CreateBudget.cshtml'), ['of-wizard-steps', 'asp-validation-summary', 'data-budget-wizard']);
      requireAll(page('Documents/New.cshtml.cs'), ['[Authorize', 'GetAsync', 'RedirectToPage']);
      requireAll(page('CommercialRoutine/Index.cshtml'), ['empty-state', 'of-routine-card', 'data-confirm']);
      requireAll(page('CommercialRoutine/Index.cshtml.cs'), ['[Authorize', 'OnPost']);
    },
    operational() {
      requireAll(page('WorkOrders/Details.cshtml.cs'), [
        'x.AccountId == account.AccountId', 'x.IsRequired && !x.IsCompleted', 'CancellationReason',
        'OnPostCompleteAsync', 'OnPostCancelAsync'
      ]);
      requireAll(page('WorkOrders/Details.cshtml'), ['data-confirm', 'timeline', 'checklist']);
    },
    financial() {
      requireAll(page('Payments/Index.cshtml.cs'), ['x.AccountId==account.AccountId', '!x.IsDeleted']);
      requireAll('src/OrcaFacil.Application/Security/PermissionCodes.cs', [
        'PaymentsManualConfirm', 'ReceiptsManage', 'ReportsFinancial', 'SensitiveDataView'
      ]);
    },
    portals() {
      requireAll('src/OrcaFacil.Application/Quality/QualityGateService.cs', ['Portal/Index', 'PartnerPortal/Index']);
      requireAll('src/OrcaFacil.Web/Program.cs', ['Disallow: /Portal']);
      requireAll('src/OrcaFacil.Application/Security/PermissionCodes.cs', ['PortalPaymentsView', 'PartnerPortalCommissionsView']);
    },
    schema() {
      requireAll('src/OrcaFacil.Persistence/Diagnostics/DatabaseSchemaContractService.cs', [
        'users', 'account_members', 'business_accounts', 'clients', 'documents', 'document_items',
        'budget_templates', 'audit_logs', 'email_outbox_messages', 'notifications', 'RequiredMigrations'
      ]);
      const hotfix = read('database/hotfix_schema_drift_quality_gate_v62.sql');
      if (/\bdrop\s+(table|column)\b/i.test(hotfix)) throw new Error('patch de schema contém operação destrutiva');
    },
    health() {
      requireAll(page('Admin/QualityGate.cshtml'), ['quality-gate-card', 'Próxima ação recomendada', 'aria-live']);
      requireAll(page('Diagnostico.cshtml'), ['schema', 'banco']);
      requireAll('src/OrcaFacil.Application/Quality/QualityGateService.cs', ['schema', 'CriticalRoutes']);
    },
    ui() {
      requireAll(page('Shared/_Layout.cshtml'), ['_ToastHost', '_ConfirmDialog', 'feedback.js', 'dialogs.js', 'forms.js']);
      for (const file of ['feedback.js', 'dialogs.js', 'forms.js']) read(`src/OrcaFacil.Web/wwwroot/js/${file}`);
      for (const file of ['feedback.css', 'documents.css', 'commercial.css', 'admin.css', 'mobile.css', 'responsive.css']) {
        read(`src/OrcaFacil.Web/wwwroot/css/${file}`);
      }
    },
    design() {
      const files = ['design-system.css', 'components.css', 'forms.css', 'feedback.css', 'admin.css'];
      const source = files.map(file => read(`src/OrcaFacil.Web/wwwroot/css/${file}`)).join('\n').toLowerCase();
      for (const token of ['page-shell', 'metric-card', 'status-badge', 'validation-summary', 'form-panel',
        'empty-state', 'loading-state', 'premium-table', 'wizard-steps', 'timeline', 'quality-gate-card']) {
        if (!source.includes(token)) throw new Error(`Design System V6.3: componente ausente: ${token}`);
      }
      if (!source.includes('prefers-reduced-motion')) throw new Error('Design System V6.3 não respeita movimento reduzido');
    }
  };

  if (mode === 'all') Object.values(checks).forEach(check => check());
  else if (checks[mode]) checks[mode]();
  else throw new Error(`modo V6.3 desconhecido: ${mode}`);
  console.log(`Homologação V6.3 (${mode}): OK`);
}
