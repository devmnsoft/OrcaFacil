import fs from 'node:fs';
import path from 'node:path';

const root = process.cwd();
const read = file => fs.readFileSync(path.join(root, file), 'utf8');
const exists = file => fs.existsSync(path.join(root, file));

const scopes = {
  enterprise: [
    ['src/OrcaFacil.Domain/Entities/SaasEnterprise.cs', ['AccountModuleSubscription', 'AccountModuleEntitlement', 'AccountModuleUsageEvent']],
    ['src/OrcaFacil.Persistence/Migrations/20260908010000_SaasEnterpriseMultiTenantModulesV66.cs', ['hotfix_saas_enterprise_modules_v66.sql']],
    ['database/hotfix_saas_enterprise_modules_v66.sql', ['saas_modules', 'account_module_subscriptions', 'account_module_entitlements']]
  ],
  superadmin: [
    ['src/OrcaFacil.Web/Pages/SuperAdmin/Index.cshtml', ['Clientes ativos', 'MonthlyRecurringRevenue']],
    ['src/OrcaFacil.Web/Pages/SuperAdmin/Clients/Index.cshtml.cs', ['AsNoTracking', 'BusinessAccounts']],
    ['src/OrcaFacil.Web/Pages/SuperAdmin/Modules/Index.cshtml.cs', ['SaasModules']]
  ],
  accountadmin: [
    ['src/OrcaFacil.Web/Pages/AccountAdmin/Index.cshtml', ['Administração da conta']],
    ['src/OrcaFacil.Web/Pages/AccountAdmin/Users/Edit.cshtml.cs', ['AccountId==account', 'AccountMember']],
    ['src/OrcaFacil.Application/Accounts/Admin/ProfilePermissionMatrix.cs', ['CanAssign']]
  ],
  entitlements: [
    ['src/OrcaFacil.Persistence/Services/Saas/ModuleAccessServices.cs', ['AccountModuleEntitlements', 'TrialEndsAt', 'hasPermission']],
    ['src/OrcaFacil.Web/Middleware/ModuleAccessMiddleware.cs', ['Status403Forbidden', 'access.CheckAsync']]
  ],
  pricing: [['src/OrcaFacil.Application/Saas/Billing/ModulePricingService.cs', ['Resolve', 'SubscriptionInvoicePreview']]],
  login: [
    ['src/OrcaFacil.Application/Auth/LoginIdentifierService.cs', ['LoginIdentifierKind.Cpf', 'LoginIdentifierKind.Cnpj', 'LoginIdentifierKind.Email']],
    ['src/OrcaFacil.Web/Pages/Auth/SelectAccount.cshtml', ['Qual conta você quer abrir?']]
  ],
  profiles: [['src/OrcaFacil.Application/Accounts/Admin/ProfilePermissionMatrix.cs', ['Owner', 'Administrator', 'Financial', 'Commercial', 'Operation', 'ReadOnly']]],
  menus: [['src/OrcaFacil.Web/Services/ModuleMenuComposer.cs', ['HasPermissionAsync', 'entitlements.CheckAsync', 'ISuperAdminMenuComposer']]],
  isolation: [
    ['src/OrcaFacil.Web/Services/CurrentAccountService.cs', ['EnsureAccountAccessAsync', 'AccountId']],
    ['src/OrcaFacil.Web/Pages/AccountAdmin/Users/Edit.cshtml.cs', ['x.AccountId==account']]
  ],
  usage: [['src/OrcaFacil.Persistence/Services/Saas/ModuleAccessServices.cs', ['MODULE_USAGE_TRACKING_FAILED', 'AccountModuleUsageEvents']]],
  health: [['src/OrcaFacil.Persistence/Diagnostics/DatabaseDiagnosticsService.cs', ['saas_modules', 'account_module_entitlements']]],
  quality: [['src/OrcaFacil.Application/Quality/QualityGateService.cs', ['modules.backend-guard', 'modules.menu', 'CheckRegistrationContractAsync']]]
};

export function checkV66(scope) {
  const contracts = scopes[scope];
  if (!contracts) throw new Error(`Escopo V6.6 desconhecido: ${scope}`);
  for (const [file, tokens] of contracts) {
    if (!exists(file)) throw new Error(`Arquivo obrigatório ausente: ${file}`);
    const source = read(file);
    for (const token of tokens) if (!source.includes(token)) throw new Error(`${file}: contrato ausente '${token}'.`);
    if (/Math\.random|href\s*=\s*["'](?:#|javascript:void|)["']/i.test(source)) throw new Error(`${file}: placeholder ou dado aleatório detectado.`);
  }
  console.log(`check:${scope}: SaaS Enterprise V6.6 validado (${contracts.length} contratos reais).`);
}
