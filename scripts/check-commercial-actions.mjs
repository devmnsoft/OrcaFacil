import { existsSync, readFileSync } from 'node:fs';

const failures = [];
const files = {
  entity: 'src/OrcaFacil.Domain/Entities/CommercialInteraction.cs',
  service: 'src/OrcaFacil.Web/Services/CommercialAutomationService.cs',
  journey: 'src/OrcaFacil.Persistence/Services/CommercialJourneyService.cs',
  page: 'src/OrcaFacil.Web/Pages/CommercialRoutine/Index.cshtml',
  model: 'src/OrcaFacil.Web/Pages/CommercialRoutine/Index.cshtml.cs',
  dashboard: 'src/OrcaFacil.Web/Pages/Dashboard/Index.cshtml',
  client: 'src/OrcaFacil.Web/Pages/Clients/Details.cshtml',
  pipeline: 'src/OrcaFacil.Web/Pages/CommercialPipeline/Index.cshtml'
};

const source = {};
for (const [name, file] of Object.entries(files)) {
  if (!existsSync(file)) failures.push(`${file}: arquivo obrigatório ausente`);
  else source[name] = readFileSync(file, 'utf8');
}

const contracts = [
  ['entity', /class CommercialInteraction[\s\S]*?AccountId[\s\S]*?ClientId[\s\S]*?DocumentId[\s\S]*?NextFollowUpAt[\s\S]*?CompletedAt/, 'entidade comercial sem vínculos, prazo ou conclusão'],
  ['service', /Where\(x => x\.AccountId == AccountId/, 'consulta da rotina sem isolamento por conta'],
  ['service', /FollowUpStatus\.Completed/, 'rotina não retira follow-up concluído das pendências'],
  ['journey', /ScheduleFollowUpAsync[\s\S]*?x\.AccountId == AccountId/, 'agendamento sem validação da conta'],
  ['journey', /CompleteFollowUpAsync[\s\S]*?x\.AccountId == AccountId/, 'conclusão sem validação da conta'],
  ['model', /OnPostScheduleAsync/, 'handler real de agendamento ausente'],
  ['model', /OnPostCompleteAsync/, 'handler real de conclusão ausente'],
  ['page', /asp-page-handler="Schedule"/, 'formulário de agendamento ausente'],
  ['page', /asp-page-handler="Complete"/, 'ação de conclusão ausente'],
  ['dashboard', /CommercialRoutine\/Index/, 'dashboard sem acesso à rotina comercial'],
  ['client', /(?:Follow-up|follow-up)/, 'Cliente 360 sem follow-up'],
  ['pipeline', /(?:Follow-up|follow-up)/, 'pipeline sem ação de follow-up']
];
for (const [key, pattern, message] of contracts) {
  if (source[key] && !pattern.test(source[key])) failures.push(`${files[key]}: ${message}`);
}

if (source.page) {
  for (const match of source.page.matchAll(/<form\b[^>]*method="post"[^>]*>/gi)) {
    if (!/asp-page-handler=/.test(match[0])) failures.push(`${files.page}: POST comercial sem handler explícito`);
  }
}

if (failures.length) {
  console.error(`Ações comerciais reprovadas:\n${failures.join('\n')}`);
  process.exit(1);
}
console.log('Ações comerciais: persistência, isolamento por conta, agenda, conclusão e superfícies validados.');
