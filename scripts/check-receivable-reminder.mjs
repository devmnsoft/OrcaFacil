import fs from 'node:fs';

// Valida a cobrança do cliente por lembrete (item 7 do backlog), sem repasse de dinheiro.
// O cliente final é lembrado do saldo em aberto (e-mail e/ou WhatsApp); nenhuma cobrança é movida:
// nem pagamento criado, nem valor alterado. Cadência e limite ficam no recebimento; o e-mail é
// idempotente por (recebimento, dia) e os dois canais respeitam opt-out.

const fail = (m) => { throw new Error(`check:receivable-reminder: ${m}`); };
const read = (p) => fs.readFileSync(p, 'utf8');

const rules = read('src/OrcaFacil.Application/Payments/ReceivableReminderRules.cs');
const contracts = read('src/OrcaFacil.Application/Payments/ReceivableReminderContracts.cs');
const options = read('src/OrcaFacil.Application/Payments/ReceivableReminderOptions.cs');
const service = read('src/OrcaFacil.Web/Services/ReceivableReminderService.cs');
const worker = read('src/OrcaFacil.Web/Services/ReceivableReminderWorker.cs');
const program = read('src/OrcaFacil.Web/Program.cs');
const tests = read('tests/OrcaFacil.UnitTests/ReceivableReminderRulesTests.cs');

// ---------- 1. Regras puras e usadas pelo serviço ----------
for (const token of ['IsOpen', 'OpenBalance', 'GraceElapsed', 'DueForNextReminder', 'UnderLimit', 'ShouldRemind'])
  if (!rules.includes(token)) fail(`regra ausente: ${token}.`);
if (!service.includes('ReceivableReminderRules.ShouldRemind')) fail('serviço não decide por regra pura.');

// ---------- 2. Sem repasse de dinheiro ----------
// O lembrete não pode alterar valor/saldo nem registrar pagamento.
if (/\.Amount\s*=[^=]/.test(service) || /\.PaidAmount\s*=[^=]/.test(service))
  fail('lembrete está alterando Amount/PaidAmount (isso moveria dinheiro).');
if (/new ManualPayment|ManualPayments\.Add|RegisterAsync/.test(service))
  fail('lembrete está registrando pagamento.');
if (!/\.LastReminderAt\s*=\s*today/.test(service))
  fail('cadência não grava a data do último lembrete no recebimento.');
if (!/\.ReminderCount\s*\+=\s*1/.test(service))
  fail('limite não incrementa o contador de lembretes.');

// ---------- 3. Opt-out respeitado nos dois canais ----------
if (!service.includes('Channel == "Email"') || !service.includes('IdentityHash == emailHash'))
  fail('e-mail não respeita opt-out comercial.');
if (!service.includes('whatsapp.SendAsync')) fail('WhatsApp sem reutilizar o envio que já trata opt-out/conta.');

// ---------- 4. E-mail idempotente e só com SMTP habilitado ----------
if (!service.includes('receivable-reminder:{entry.Id:N}:{today:yyyyMMdd}'))
  fail('e-mail sem chave de idempotência por (recebimento, dia).');
if (!/\.EmailSendingEnabled\s*==\s*true/.test(service)) fail('e-mail enviado sem checar se a conta tem envio habilitado.');

// ---------- 5. Só lembra quem dá para alcançar ----------
if (!service.includes('if (!emailOk && !waOk) continue;'))
  fail('lembre lembrando quem não tem nenhum canal (contaria sem chegar a ninguém).');

// ---------- 6. Worker com trava e data civil ----------
if (!worker.includes('"receivable_reminder"')) fail('worker sem nome de trava estável.');
if (!worker.includes('IJobLockService')) fail('worker sem trava de concorrência.');
if (!worker.includes('CommercialClock.BusinessTimeZone')) fail('worker não usa a data civil de São Paulo.');

// ---------- 7. DI e opção de cadência ----------
for (const token of ['IReceivableReminderService, ReceivableReminderService', 'AddHostedService<ReceivableReminderWorker>', 'Configure<ReceivableReminderOptions>'])
  if (!program.includes(token)) fail(`${token} sem registro no Program.cs.`);
if (!options.includes('MaxReminders') || !options.includes('IntervalDays'))
  fail('opções de cadência incompletas.');

// ---------- 8. Testes cobrem as fronteiras anti-spam ----------
for (const [name, token] of [
  ['recebimento aberto exige saldo', 'Open_receivable_requires_unsettled_status_and_balance'],
  ['nunca antes do vencimento', 'Grace_never_reminds_before_the_due_date'],
  ['intervalo entre lembretes', 'Cadence_respects_the_interval_between_reminders'],
  ['limite encerra', 'Cap_stops_further_reminders'],
])
  if (!tests.includes(token)) fail(`teste ausente: ${name}.`);

console.log('Cobrança do cliente — lembrete (V7.9): e-mail idempotente + WhatsApp com opt-out, cadência e ' +
  'limite no recebimento, sem mover dinheiro, worker com trava na data civil, DI e testes anti-spam OK.');
