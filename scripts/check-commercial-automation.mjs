import { access, readFile } from 'node:fs/promises';

const contracts = [
  ['src/OrcaFacil.Web/Pages/CommercialPipeline/Index.cshtml', ['asp-page="/Documents/Details"', 'data-pipeline-stage', 'data-pipeline-client']],
  ['src/OrcaFacil.Persistence/Services/CommercialWorkspaceQueryService.cs', ['x.AccountId == AccountId', 'AsNoTracking()', '"paid"', '"lost"']],
  ['src/OrcaFacil.Web/Services/CommercialAutomationService.cs', ['GetTemplatesAsync']],
  ['src/OrcaFacil.Persistence/Services/CommercialJourneyService.cs', ['ScheduleFollowUpAsync']],
  ['src/OrcaFacil.Web/Pages/MessageTemplates/Index.cshtml.cs', ['OnPostSaveAsync']],
  ['src/OrcaFacil.Domain/Entities/CommercialInteraction.cs', ['NextFollowUpAt', 'CommercialMessageTemplate']]
];
const failures = [];
for (const [file, tokens] of contracts) {
  await access(file);
  const source = await readFile(file, 'utf8');
  for (const token of tokens) if (!source.includes(token)) failures.push(`${file}: contrato ausente: ${token}`);
}
if (failures.length) {
  console.error(`Automação comercial incompleta:\n${failures.join('\n')}`);
  process.exit(1);
}
console.log('Automação comercial validada: pipeline account-scoped, follow-up persistente e templates com gravação real.');
