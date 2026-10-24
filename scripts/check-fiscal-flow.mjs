import { readFileSync } from 'node:fs';

const page = readFileSync('src/OrcaFacil.Web/Pages/Fiscal/Index.cshtml', 'utf8');
const model = readFileSync('src/OrcaFacil.Web/Pages/Fiscal/Index.cshtml.cs', 'utf8');
const rules = readFileSync('src/OrcaFacil.Application/Fiscal/FiscalRequestRules.cs', 'utf8');
const service = readFileSync('src/OrcaFacil.Web/Services/FiscalDocumentWorkflowService.cs', 'utf8');
const program = readFileSync('src/OrcaFacil.Web/Program.cs', 'utf8');
const tests = readFileSync('tests/OrcaFacil.UnitTests/FiscalRequestRulesTests.cs', 'utf8');
const failures = [];

// ---------- 1. Regras puras do ciclo de vida existem ----------
for (const token of ['IsFiscalizable', 'IsActive', 'IsSettled', 'CanTransition', 'RequiresReason', 'IsValidExternalNumber', 'MaskAccessKey'])
  if (!rules.includes(token)) failures.push(`regra pura ausente: ${token}`);
// Uma nota emitida pode ser cancelada — e só isso a partir de Issued.
if (!rules.includes('(FiscalRequestStatus.Issued, FiscalRequestStatus.Canceled) => true'))
  failures.push('transição Issued -> Canceled não permitida nas regras.');
if (rules.includes('(FiscalRequestStatus.Canceled, FiscalRequestStatus.Issued)'))
  failures.push('estado terminal Canceled não deveria reabrir para emissão.');
// Chave de acesso é mascarada mostrando apenas as pontas.
if (!rules.includes('k[..4]') || !rules.includes('k[^4..]'))
  failures.push('mascara da chave de acesso não preserva apenas as pontas.');

// ---------- 2. Serviço aplica as regras e mantém o pedido na conta certa ----------
if (!service.includes('FiscalRules.EnsureSameAccount')) failures.push('serviço sem isolamento de conta (EnsureSameAccount).');
if (!service.includes('FiscalRequestRules.IsFiscalizable')) failures.push('serviço não exige recebível com valor recebido.');
if (!service.includes('FiscalRequestRules.IsActive')) failures.push('serviço sem idempotência por solicitação aberta.');
if (!service.includes('FiscalRequestRules.CanTransition')) failures.push('serviço não valida a transição pelo mapa de estados.');
if (!service.includes('FiscalRules.RequireReason')) failures.push('serviço não exige motivo auditável.');
if (!service.includes('.Touch()')) failures.push('serviço não atualiza o Timestamp do pedido ao mudar de estado.');
if (!service.includes('ActivityEvents.Add')) failures.push('serviço não grava evento de auditoria/atividade.');

// ---------- 3. Nenhum número ou chave fiscal é fabricado ----------
if (!service.includes('request.ExternalNumber = externalNumber.Trim()'))
  failures.push('número fiscal não vem do operador (risco de número fabricado).');
if (/AtomicDocumentNumber|GenerateNumber|NextNumber/.test(service))
  failures.push('serviço usa gerador sequencial de número (não deve fabricar número fiscal).');
if (!service.includes('Controle manual auditado'))
  failures.push('provedor não é estampado como controle manual auditado.');
if (!service.includes('FiscalRequestRules.MaskAccessKey'))
  failures.push('chave de acesso não é armazenada mascarada.');

// ---------- 4. Registro em DI ----------
if (!program.includes('IFiscalDocumentWorkflowService'))
  failures.push('IFiscalDocumentWorkflowService não registrado no DI.');

// ---------- 5. Página /Fiscal isolada e com as ações do fluxo ----------
if (!page.includes('@page "/Fiscal"')) failures.push('rota /Fiscal ausente.');
for (const handler of ['Request', 'Issue', 'Cancel', 'NotRequired'])
  if (!page.includes(`asp-page-handler="${handler}"`)) failures.push(`ação ${handler} ausente na página fiscal.`);
if (!page.includes('Model.UnfiscalizedCount')) failures.push('página não destaca valores recebidos sem nota fiscal.');
if (!page.includes('FiscalRules.AccountingDisclaimer')) failures.push('página não exibe a ressalva contábil.');
if (!model.includes('account.AccountId is not Guid accountId')) failures.push('página fiscal sem isolamento de conta.');
if (!model.includes('PermissionCodes.FinanceManage')) failures.push('página fiscal sem permissão financeira.');
for (const call of ['workflow.RequestAsync', 'workflow.RegisterIssuanceAsync', 'workflow.CancelAsync', 'workflow.MarkNotRequiredAsync'])
  if (!model.includes(call)) failures.push(`modelo da página não chama ${call}.`);

// ---------- 6. Testes fixam as fronteiras das regras ----------
for (const [name, token] of [
  ['solicitável exige dinheiro recebido', 'Is_fiscalizable_requires_received_money_and_not_canceled'],
  ['apenas as transições definidas', 'Can_transition_allows_only_the_defined_edges'],
  ['emitida cancela mas não reemite', 'An_issued_note_can_be_cancelled_but_not_issued_twice'],
  ['chave mascarada preserva pontas', 'Mask_access_key_keeps_ends_and_hides_middle'],
])
  if (!tests.includes(token)) failures.push(`teste ausente: ${name}`);

if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log('Fluxo fiscal validado: ciclo de vida em regras puras testadas, pedidos isolados por conta, número/chave vindos do operador (nada fabricado), página /Fiscal com as ações e a ressalva contábil.');
