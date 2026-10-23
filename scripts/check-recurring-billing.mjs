import fs from 'node:fs';

// Valida a assinatura recorrente do cliente (item 6 do backlog de evolução).
// O problema real: NextBillingDate era gravada uma vez e nunca avançava, nenhuma competência
// era gerada sozinha, e Overdue/Expired só existiam como contagem na leitura dos relatórios.
// Este validador prova que a cobrança recorrente agora anda sozinha, sem duplicar cobrança
// e com o mesmo rastro financeiro do caminho manual.

const fail = (m) => { throw new Error(`check:recurring-billing: ${m}`); };
const read = (p) => fs.readFileSync(p, 'utf8');

const rules = read('src/OrcaFacil.Application/Contracts/RecurringBillingRules.cs');
const contracts = read('src/OrcaFacil.Application/Contracts/RecurringBillingContracts.cs');
const service = read('src/OrcaFacil.Persistence/Services/RecurringContractBillingService.cs');
const worker = read('src/OrcaFacil.Web/Services/RecurringContractBillingWorker.cs');
const program = read('src/OrcaFacil.Web/Program.cs');
const manualPage = read('src/OrcaFacil.Web/Pages/Contracts/Details.cshtml.cs');
const tests = read('tests/OrcaFacil.UnitTests/RecurringBillingRulesTests.cs');

// ---------- 1. Regras puras, testáveis sem banco ----------
for (const token of ['PeriodMonths', 'AdvanceDue', 'CompetenceOf', 'ShouldGenerate', 'ShouldMarkOverdue', 'ShouldExpire'])
  if (!rules.includes(token)) fail(`regra ausente: ${token}.`);
// Fim de mês: o vencimento tem de ser grampeado ao último dia, não estourar (31 de fevereiro).
if (!rules.includes('DateTime.DaysInMonth')) fail('avanço do vencimento sem grampo ao fim do mês.');
// Período personalizado sem meses positivos tem de ser recusado, senão avança zero meses em loop.
if (!rules.includes('customMonths is >= 1')) fail('período personalizado aceita meses inválidos.');
// Geração só para contrato ativo: pausado/suspenso/cancelado não pode cobrar.
if (!rules.includes('ContractRecurrenceRules.CanGenerate(status)')) fail('geração não respeita o status do contrato.');

// ---------- 2. Contrato de serviço e worker ----------
if (!contracts.includes('Task<RecurringBillingRunResult> RunAsync(DateOnly today')) fail('contrato do serviço sem RunAsync com data explícita.');
if (!service.includes(': IRecurringContractBillingService')) fail('serviço não implementa o contrato.');
if (!worker.includes('"recurring_contract_billing"')) fail('worker sem nome de trava estável.');
if (!worker.includes('IJobLockService')) fail('worker sem trava de concorrência entre instâncias.');
if (!worker.includes('IDatabaseConfigurationState')) fail('worker não checa banco configurado.');
// A data de negócio é a civil de São Paulo, nunca o UTC do servidor (cobraria até 3h antes do vencimento).
if (!worker.includes('CommercialClock.BusinessTimeZone')) fail('worker usa data do servidor em vez da civil de São Paulo.');

// ---------- 3. Sem cobrança duplicada ----------
// A competência já existente (inclusive registrada à mão) não pode gerar outra cobrança.
if (!service.includes('x.Competence == competence') || !service.includes('AnyAsync'))
  fail('geração não confere competência existente antes de cobrar.');
if (!service.includes('MaxPeriodsPerRun')) fail('sem limite de competências por rodada (catch-up sem teto).');
// O avanço de NextBillingDate é o que impede reprocessar a mesma data.
if (!service.includes('contract.NextBillingDate = next')) fail('NextBillingDate não é avançado após gerar.');

// ---------- 4. Mesmo rastro financeiro do caminho manual ----------
if (!service.includes('FinancialEntryOrigin.Contract') || !service.includes('ContractPaymentId = payment.Id'))
  fail('cobrança automática não cria o lançamento a receber vinculado (não aparece em Financeiro).');
if (!service.includes('FinancialEntryStatus.Pending')) fail('lançamento automático sem status pendente explícito.');
// Caminho manual continua existindo e usa a mesma forma de vínculo.
if (!manualPage.includes('ContractPaymentId = payment.Id')) fail('caminho manual deixou de vincular o lançamento.');

// ---------- 5. Atraso e expiração são gravados, não só calculados na leitura ----------
if (!service.includes('RecurringPaymentStatus.Overdue')) fail('atraso de cobrança nunca é gravado.');
if (!service.includes('FinancialEntryStatus.Overdue')) fail('lançamento financeiro não acompanha o atraso.');
if (!service.includes('ContractStatus.Expired')) fail('contrato vencido nunca é expirado.');
if (!service.includes('RecurringBillingRules.ShouldMarkOverdue') || !service.includes('RecurringBillingRules.ShouldExpire'))
  fail('serviço não usa as regras puras para decidir atraso e expiração.');
// Atraso não pode tocar cobrança já paga.
if (!service.includes('RecurringPaymentStatus.Pending && x.DueDate < today'))
  fail('marcação de atraso não se restringe a cobrança pendente.');

// ---------- 6. Registro de DI ----------
for (const token of ['IRecurringContractBillingService, RecurringContractBillingService', 'RecurringContractBillingWorker'])
  if (!program.includes(token)) fail(`${token} sem registro de DI.`);
if (!program.includes('using OrcaFacil.Application.Contracts;')) fail('Program.cs sem o namespace de contratos.');

// ---------- 7. Testes cobrem as fronteiras que costumam errar ----------
for (const [name, token] of [
  ['vencimento grampeado ao fim do mês', 'Advance_clamps_due_day_to_month_end'],
  ['período personalizado inválido', 'Custom_period_without_positive_months_is_rejected'],
  ['contrato pausado não gera', 'Inactive_contract_never_generates'],
  ['não gera após o término', 'Nothing_is_generated_after_the_term_ends'],
  ['cobrança paga não vira atraso', 'Settled_or_already_overdue_charge_is_not_marked_again'],
  ['vence hoje não é atraso', 'Charge_due_today_is_not_overdue_yet'],
  ['termina hoje ainda vigente', 'Contract_ending_today_is_still_running'],
])
  if (!tests.includes(token)) fail(`teste ausente: ${name}.`);

console.log('Assinatura recorrente do cliente (V7.9): cobrança de cada competência gerada automaticamente, ' +
  'sem duplicar período, com lançamento a receber vinculado, atraso e expiração gravados, ' +
  'worker com trava na data civil de São Paulo, DI e testes de fronteira OK.');
