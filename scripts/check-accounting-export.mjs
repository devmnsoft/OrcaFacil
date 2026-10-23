import { readFileSync } from 'node:fs';

const page = readFileSync('src/OrcaFacil.Web/Pages/Fiscal/Index.cshtml.cs', 'utf8');
const view = readFileSync('src/OrcaFacil.Web/Pages/Fiscal/Index.cshtml', 'utf8');
const map = readFileSync('src/OrcaFacil.Application/Fiscal/FiscalAccountingSnapshot.cs', 'utf8');
const tests = readFileSync('tests/OrcaFacil.UnitTests/FiscalAccountingSnapshotTests.cs', 'utf8');
const failures = [];

// ---------- 1. O export passa pela mesma guarda de acesso da página ----------
if (!page.includes('OnGetExportAsync')) failures.push('página /Fiscal sem o handler de export (OnGetExportAsync).');
if (!/OnGetExportAsync[\s\S]{0,400}?CanAccessAsync\(ct\)/.test(page)) failures.push('export não passa pela guarda de acesso da página (CanAccessAsync).');
if (!page.includes('account.AccountId is not Guid accountId')) failures.push('export sem isolamento de conta.');
if (!page.includes('PermissionCodes.FinanceManage')) failures.push('página fiscal sem permissão financeira.');

// ---------- 2. Só notas EMITIDAS e só da conta (query do export) ----------
if (!page.includes('.Where(x => x.AccountId == accountId && !x.IsDeleted && x.Status == FiscalRequestStatus.Issued)'))
  failures.push('export não isola as notas EMITIDAS da conta (query do export).');

// ---------- 3. Reusa o serviço compartilhado (CSV + JSON) e devolve arquivo ----------
for (const tok of ['FiscalAccountingExportService', 'ExportCsv', 'ExportJson'])
  if (!page.includes(tok)) failures.push(`export não reusa ${tok}.`);
if (!page.includes('text/csv') || !page.includes('application/json')) failures.push('export sem os tipos CSV e JSON.');
if (!page.includes('File(')) failures.push('export não devolve um arquivo (File(...)).');

// ---------- 4. A competência é medida no fuso comercial (nunca no servidor) ----------
if (!page.includes('CommercialClock.NormalizeToUtc')) failures.push('competência não normaliza o instante no UTC comercial.');
if (!page.includes('ScheduleAgendaRules.BusinessToday')) failures.push('competência não compara o dia civil no fuso comercial.');

// ---------- 5. Exportar é um evento auditado ----------
if (!page.includes('ActivityEvents.Add')) failures.push('export não grava evento de auditoria/atividade.');
if (!page.includes('"FiscalAccountingExported"')) failures.push('evento de auditoria do export ausente (FiscalAccountingExported).');

// ---------- 6. Mapeador puro: só dados registrados, nada fabricado ----------
for (const tok of ['FiscalAccountingSource', 'ToSnapshot'])
  if (!map.includes(tok)) failures.push(`mapeador puro ausente: ${tok}.`);
if (!map.includes('FiscalDocumentStatus.ManualRegistered')) failures.push('snapshot não mapeia para ManualRegistered (controle manual, sem autorização de prefeitura).');
if (!map.includes('(sem número)')) failures.push('número ausente não é sinalizado (deve ser "(sem número)", nunca fabricado).');

// ---------- 7. A view expõe o controle de export (CSV e JSON + competência) ----------
if (!view.includes('asp-page-handler="Export"')) failures.push('view sem o formulário de export (handler Export).');
if (!view.includes('value="csv"') || !view.includes('value="json"')) failures.push('view sem as opções CSV e JSON.');
if (!view.includes('name="desde"') || !view.includes('name="ate"')) failures.push('view sem o filtro de competência (desde/até).');

// ---------- 8. Testes fixam as fronteiras do mapeamento ----------
for (const [name, token] of [
  ['mapeia todos os campos registrados', 'Maps_all_recorded_fields_to_the_snapshot'],
  ['número ausente é sinalizado', 'Missing_number_is_flagged_not_fabricated'],
  ['só emitidas são exportáveis', 'Only_issued_requests_are_exportable'],
])
  if (!tests.includes(token)) failures.push(`teste do mapeador ausente: ${name}`);

if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log('Export contábil validado: pacote CSV/JSON das notas emitidas, isolado por conta, competência no fuso comercial, serviço compartilhado reutilizado, evento auditado e view com o controle de export.');
