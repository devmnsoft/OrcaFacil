import { existsSync, readFileSync } from 'node:fs';
const failures=[]; const files=['WorkOrders/Index','WorkOrders/Details','Schedule/Index','Reports/Operational'];
for(const page of files) for(const ext of ['cshtml','cshtml.cs']) { const path=`src/OrcaFacil.Web/Pages/${page}.${ext}`; if(!existsSync(path)) failures.push(`${path}: rota ausente`); }
const service=readFileSync('src/OrcaFacil.Persistence/Services/CommercialJourneyService.cs','utf8');
for(const contract of ['ConvertToWorkOrderAsync','ScheduleAsync','StartAsync','PauseAsync','ResumeAsync','CompleteAsync','CancelAsync','FinancialEntries']) if(!service.includes(contract)) failures.push(`fluxo sem ${contract}`);
if(failures.length){console.error(failures.join('\n'));process.exit(1)} console.log('Fluxo operacional real validado.');
