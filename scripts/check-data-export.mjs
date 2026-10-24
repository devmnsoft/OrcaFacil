import { readFileSync } from 'node:fs';

const shaper = readFileSync('src/OrcaFacil.Application/Privacy/AccountDataBackup.cs', 'utf8');
const service = readFileSync('src/OrcaFacil.Application/Privacy/DataExportService.cs', 'utf8');
const page = readFileSync('src/OrcaFacil.Web/Pages/Settings/Privacy/Index.cshtml.cs', 'utf8');
const view = readFileSync('src/OrcaFacil.Web/Pages/Settings/Privacy/Index.cshtml', 'utf8');
const usage = readFileSync('src/OrcaFacil.Application/Plans/UserUsageService.cs', 'utf8');
const tests = readFileSync('tests/OrcaFacil.UnitTests/AccountDataBackupTests.cs', 'utf8');
const failures = [];

// ---------- 1. Mapeador puro forma o bundle (sem DbContext) ----------
for (const tok of ['AccountDataBackupShaper', 'Shape(', 'Scope(', 'TotalRecords(', 'AccountDataBackup'])
  if (!shaper.includes(tok)) failures.push(`mapeador puro ausente: ${tok}.`);

// ---------- 2. Regra de inclusão financeira + tag de escopo ----------
if (!shaper.includes('includesFinancial && financialEntries is not null'))
  failures.push('lançamentos financeiros só entram no bundle quando incluídos e disponíveis.');
if (!shaper.includes('"account.full"') || !shaper.includes('"account.core"'))
  failures.push('tag de escopo não distingue bundle core de bundle full.');

// ---------- 3. A página liga o serviço de export já existente ----------
if (!page.includes('OnGetExportAsync')) failures.push('página Privacidade sem o handler OnGetExportAsync.');
if (!page.includes('exportService.ExportJsonAsync')) failures.push('página não chama o DataExportService.ExportJsonAsync.');

// ---------- 4. Gate da permissão de export de dados ----------
if (!page.includes('current.HasPermissionAsync(PermissionCodes.PrivacyExportData'))
  failures.push('export não exige a permissão Privacy.ExportData.');

// ---------- 5. Dados financeiros exigem permissão financeira ----------
if (!page.includes('PermissionCodes.FinanceManage')) failures.push('inclusão financeira sem gate de permissão financeira.');
if (!page.includes('includeFin = includeFinancial && canExportFinancial'))
  failures.push('inclusão financeira deve depender do pedido E da permissão.');

// ---------- 6. Tudo isolado por conta ----------
if (!page.includes('db.Clients.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted)'))
  failures.push('clientes não isolados por conta no export.');
if (!page.includes('db.FinancialEntries.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted)'))
  failures.push('lançamentos financeiros não isolados por conta no export.');

// ---------- 7. Incrementa o medidor BackupExports antes órfão ----------
if (!page.includes('usage.RegisterBackupExportAsync')) failures.push('export não incrementa o medidor (usage.RegisterBackupExportAsync).');
if (!usage.includes('item.BackupExports++')) failures.push('UserUsageService não incrementa BackupExports.');

// ---------- 8. Devolve o arquivo ao navegador ----------
if (!page.includes('File(result.Content, result.ContentType, result.FileName)'))
  failures.push('export não devolve o arquivo gerado (File(...)).');

// ---------- 9. A view expõe o controle, gateado pela permissão ----------
if (!view.includes('Model.CanExport')) failures.push('view não condiciona o bloco de export à permissão (CanExport).');
if (!view.includes('asp-page-handler="Export"')) failures.push('view sem o formulário de export (handler Export).');
if (!view.includes('name="includeFinancial"')) failures.push('view sem a opção de incluir dados financeiros.');

// ---------- 10. A promessa "nunca inclui senha/hash/token/segredo" segue valendo ----------
if (!service.includes('sanitizer.SanitizeJson')) failures.push('serviço de export parou de sanitizar dados sensíveis.');

// ---------- 11. Testes fixam as fronteiras do mapeamento ----------
for (const [name, token] of [
  ['financeira excluída por padrão', 'Financial_excluded_when_not_included'],
  ['financeira incluída quando permitida', 'Financial_included_when_permitted_and_requested'],
  ['conta vazia gera listas vazias', 'Empty_account_produces_empty_lists_and_zero_records'],
])
  if (!tests.includes(token)) failures.push(`teste do backup ausente: ${name}`);

if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log('Backup exportável validado: export JSON self-service na Privacidade, gateado por Privacy.ExportData, financeiro só com permissão, isolado por conta, sem segredos, medido por BackupExports.');
