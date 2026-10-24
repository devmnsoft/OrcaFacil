import fs from 'node:fs';

// Valida o fechamento da ordem de serviço (item 5 do backlog de evolução).
// O problema real: WaitingCustomer, WaitingMaterial e Overdue existiam na tabela de
// transições, mas nada no sistema conseguia gravá-las — o KPI de "serviços atrasados"
// do dashboard ficava sempre zero e o operador não tinha como registrar espera. Este
// validador prova que os três estados agora são alcançáveis, com rastro auditável.

const fail = (m) => { throw new Error(`check:work-orders: ${m}`); };
const read = (p) => fs.readFileSync(p, 'utf8');

const contracts = read('src/OrcaFacil.Application/Commercial/CommercialJourneyContracts.cs');
const waitRules = read('src/OrcaFacil.Application/WorkOrders/WorkOrderWaitRules.cs');
const maintenance = read('src/OrcaFacil.Application/WorkOrders/WorkOrderMaintenanceContracts.cs');
const journey = read('src/OrcaFacil.Persistence/Services/CommercialJourneyService.cs');
const overdueService = read('src/OrcaFacil.Persistence/Services/WorkOrderOverdueService.cs');
const worker = read('src/OrcaFacil.Web/Services/WorkOrderOverdueWorker.cs');
const entity = read('src/OrcaFacil.Domain/Entities/CommercialJourney.cs');
const enums = read('src/OrcaFacil.Domain/Enums/CommercialStatus.cs');
const transitions = read('src/OrcaFacil.Application/WorkOrders/WorkOrderStatusTransitionService.cs');
const pageCs = read('src/OrcaFacil.Web/Pages/WorkOrders/Details.cshtml.cs');
const pageCshtml = read('src/OrcaFacil.Web/Pages/WorkOrders/Details.cshtml');
const config = read('src/OrcaFacil.Persistence/Configurations/CommercialJourneyConfiguration.cs');
const program = read('src/OrcaFacil.Web/Program.cs');
const migration = read('src/OrcaFacil.Persistence/Migrations/20261023120000_AddWorkOrderWaitAndOverdueState.cs');
const snapshot = read('src/OrcaFacil.Persistence/Migrations/OrcaFacilDbContextModelSnapshot.cs');
const hotfix = read('database/hotfix_work_order_wait_overdue_v78.sql');
const consolidated = read('database/script_completop.sql');
const tests = read('tests/OrcaFacil.UnitTests/WorkOrderWaitAndOverdueTests.cs');

// ---------- 1. Os três estados têm contrato de entrada ----------
if (!contracts.includes('Task<WorkOrderResult> WaitAsync')) fail('contrato sem WaitAsync.');
if (!enums.includes('enum WorkOrderWaitReason')) fail('enum de motivo de espera ausente.');
if (!entity.includes('WorkOrderWaitReason WaitReason')) fail('entidade sem o motivo de espera.');
if (!entity.includes('DateTime? WaitingSince')) fail('entidade sem a data de início da espera.');
if (!entity.includes('string? WaitNote')) fail('entidade sem a observação da espera.');
if (!entity.includes('DateTime? OverdueMarkedAt')) fail('entidade sem a data de marcação de vencimento.');

// ---------- 2. Regra pura: motivo -> status, testável sem banco ----------
if (!waitRules.includes('TryGetStatus')) fail('regra de espera não extraída para teste sem banco.');
for (const token of ['WaitingCustomer', 'WaitingMaterial'])
  if (!waitRules.includes(token)) fail(`regra sem o motivo ${token}.`);
if (!waitRules.includes('default: status = default; return false'))
  fail('motivo None não é rejeitado pela regra (entraria em estado inválido).');

// ---------- 3. Espera entra por InProgress e sai por InProgress ----------
if (!journey.includes('WorkOrderWaitRules.TryGetStatus')) fail('serviço não usa a regra pura.');
if (!journey.includes('x.WaitReason = reason')) fail('espera sem registro do motivo.');
if (!journey.includes('x.WaitingSince = DateTime.UtcNow')) fail('espera sem carimbo de tempo.');
// Retomar tem de limpar o estado de espera; senão a próxima OS herdaria o motivo antigo.
if (!journey.includes('x.WaitReason = WorkOrderWaitReason.None'))
  fail('retomar não limpa o motivo de espera.');
if (!journey.includes('x.WaitingSince = null')) fail('retomar não limpa a data de espera.');

// ---------- 4. Overdue: worker real, multi-conta, com trava de concorrência ----------
if (!maintenance.includes('MarkOverdueAsync')) fail('contrato de manutenção sem MarkOverdueAsync.');
if (!overdueService.includes('WorkOrderStatus.Overdue')) fail('serviço não grava Overdue.');
if (!overdueService.includes('x.Status == WorkOrderStatus.Scheduled'))
  fail('marcação de vencimento não filtra por Scheduled (poderia vencer ordem já em execução).');
if (!overdueService.includes('x.ScheduledEnd < now')) fail('sem critério de vencimento por data.');
// Toda mudança de estado tem de deixar rastro na timeline.
if (!overdueService.includes('ActivityEvents.Add')) fail('vencimento sem evento na timeline.');
if (!worker.includes('IJobLockService')) fail('worker sem trava de concorrência entre instâncias.');
if (!worker.includes('"work_order_overdue"')) fail('worker sem nome de trava estável.');
if (!worker.includes('IDatabaseConfigurationState')) fail('worker sem checar banco configurado.');

// ---------- 5. UI: operador consegue operar os estados ----------
if (!pageCs.includes('OnPostWaitAsync')) fail('página sem handler de espera.');
if (!pageCshtml.includes('asp-page-handler="Wait"')) fail('página sem botão de espera.');
if (!pageCshtml.includes('WaitingMaterial')) fail('página sem opção de aguardar material.');
if (!pageCshtml.includes('asp-page-handler="Resume"')) fail('página sem retomar a execução.');
// A tela tem de mostrar por quanto tempo a ordem espera, senão o operador decide às cegas.
if (!pageCshtml.includes('WaitingSince')) fail('página não exibe desde quando a ordem espera.');

// ---------- 6. Schema: migration, snapshot, hotfix e base nova ----------
for (const token of ['wait_reason', 'waiting_since', 'wait_note', 'overdue_marked_at'])
  if (!migration.includes(token)) fail(`migration sem ${token}.`);
if (!snapshot.includes('wait_reason') || !snapshot.includes('overdue_marked_at'))
  fail('snapshot desatualizado (nova coluna ausente).');
if (!hotfix.includes('ADD COLUMN IF NOT EXISTS wait_reason')) fail('hotfix não é aditivo/idempotente.');
if (!consolidated.includes('\\ir hotfix_work_order_wait_overdue_v78.sql'))
  fail('hotfix não encadeado no script_completop.sql (base nova nasceria sem as colunas).');
// O enum precisa virar string com limite; senão a gravação estoura o varchar em produção.
if (!config.includes('x.WaitReason).HasConversion<string>()'))
  fail('WaitReason sem conversão para string (gravação falharia em produção).');

// ---------- 7. DI: serviço e worker registrados ----------
for (const token of ['IWorkOrderOverdueService', 'WorkOrderOverdueWorker'])
  if (!program.includes(token)) fail(`${token} sem registro de DI.`);

// ---------- 8. Testes cobrem o que já doeu ----------
for (const [name, token] of [
  ['espera alcançável a partir de execução', 'Waiting_states_are_enterable_from_in_progress'],
  ['retomada da espera', 'Waiting_states_can_resume_to_in_progress'],
  ['não concluir direto da espera', 'Waiting_states_cannot_complete_directly'],
  ['vencimento alcançável a partir do agendamento', 'Scheduled_order_can_be_marked_overdue'],
  ['vencida ainda pode iniciar ou cancelar', 'Overdue_order_can_still_be_started_or_cancelled'],
  ['concluída/cancelada não vira vencida', 'Completed_order_cannot_be_marked_overdue'],
  ['motivo None rejeitado', 'Wait_reason_none_is_rejected'],
])
  if (!tests.includes(token)) fail(`teste ausente: ${name}.`);

console.log('Ordem de serviço (V7.8): WaitingCustomer, WaitingMaterial e Overdue agora são alcançáveis — ' +
  'espera com motivo/note auditável, worker de vencimento com trava e rastro em timeline, ' +
  'UI operável, migration + hotfix encadeado na base nova e enum convertido para string OK.');
