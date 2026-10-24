import { readFileSync } from 'node:fs';
const page = readFileSync('src/OrcaFacil.Web/Pages/Schedule/Index.cshtml', 'utf8');
const model = readFileSync('src/OrcaFacil.Web/Pages/Schedule/Index.cshtml.cs', 'utf8');
const rules = readFileSync('src/OrcaFacil.Application/Scheduling/ScheduleAgendaRules.cs', 'utf8');
const tests = readFileSync('tests/OrcaFacil.UnitTests/ScheduleAgendaRulesTests.cs', 'utf8');
const failures = [];

// ---------- 1. Visões da agenda e isolamento ----------
for (const view of ['today', 'week', 'overdue', 'unscheduled'])
  if (!page.includes(`view == "${view}"`) && !page.includes(`View == "${view}"`)) failures.push(`visão ${view} ausente`);
if (!model.includes('AccountId == account.AccountId')) failures.push('agenda sem isolamento de conta');
if (!model.includes('AsNoTracking')) failures.push('agenda sem leitura otimizada');

// ---------- 2. Regras puras existem com a matemática correta ----------
for (const token of ['BusinessToday', 'CivilDayStartUtc', 'Window(', 'HasWindow', 'FindConflicts'])
  if (!rules.includes(token)) failures.push(`regra pura ausente: ${token}`);
// Sobrepposição estrita, igual ao que FieldScheduleService aplica ao salvar (fronteiras em comum não conflitam).
if (!rules.includes('a.StartUtc < b.EndUtc && b.StartUtc < a.EndUtc'))
  failures.push('regra de conflito não usa sobreposição estrita por recurso.');

// ---------- 3. Bucketing por DATA CIVIL (zona de negócio), nunca pelo dia UTC do servidor ----------
if (!model.includes('ScheduleAgendaRules.BusinessToday')) failures.push('agenda não calcula o dia de negócio pela regra pura.');
if (!model.includes('ScheduleAgendaRules.Window')) failures.push('janela hoje/semana não vem da regra de data civil.');
if (!model.includes('x.ScheduledStart >= startUtc && x.ScheduledStart < endUtc'))
  failures.push('consulta não filtra pela janela civil [startUtc, endUtc).');
if (model.includes('DateTime.UtcNow.Date'))
  failures.push('agenda ainda bucketiza pelo dia UTC do servidor (defeito que este item corrige).');

// ---------- 4. Horas exibidas na zona de negócio, não no fuso do servidor ----------
if (!model.includes('CommercialClock.BusinessTimeZone'))
  failures.push('exibição não projeta as horas para a zona de negócio (America/Sao_Paulo).');
if (page.includes('.ToLocalTime()'))
  failures.push('agenda exibe horas no fuso do servidor em vez do fuso de negócio.');

// ---------- 5. Conflito de horário visível na agenda ----------
if (!model.includes('ScheduleAgendaRules.FindConflicts')) failures.push('agenda não detecta sobreposição por responsável.');
if (!page.includes('Model.ConflictCount')) failures.push('agenda não resume quantos serviços têm conflito.');
if (!page.includes('of-badge-warning')) failures.push('agenda não sinaliza visualmente o serviço com horário sobreposto.');

// ---------- 6. Testes fixam as fronteiras ----------
for (const [name, token] of [
  ['dia de negócio != dia UTC', 'Business_day_follows_the_business_zone_not_the_utc_wall_date'],
  ['meia-noite local -> UTC', 'Civil_day_start_is_local_midnight_projected_to_utc'],
  ['conflito só mesmo responsável', 'Conflict_flags_only_overlaps_booked_to_the_same_person'],
  ['janelas adjacentes não conflitam', 'Adjacent_windows_do_not_conflict'],
])
  if (!tests.includes(token)) failures.push(`teste ausente: ${name}`);

if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log('Agenda operacional validada: data civil na zona de negócio, horas projetadas para SP, conflitos por responsável visíveis, regras puras testadas.');
