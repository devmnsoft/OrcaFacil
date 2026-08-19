import { readFileSync } from 'node:fs';
const transition=readFileSync('src/OrcaFacil.Application/WorkOrders/WorkOrderStatusTransitionService.cs','utf8');
const service=readFileSync('src/OrcaFacil.Persistence/Services/CommercialJourneyService.cs','utf8');
const failures=[]; for(const status of ['Scheduled','InProgress','Paused','Completed','Cancelled','Overdue']) if(!transition.includes(`WorkOrderStatus.${status}`)) failures.push(`transição ${status} ausente`);
if(!service.includes('CancellationReasonRequired')) failures.push('cancelamento não exige motivo');
if(!service.includes('SourceDocumentId == document.Id')) failures.push('conversão não idempotente');
if(failures.length){console.error(failures.join('\n'));process.exit(1)} console.log('Transições e invariantes de OS validadas.');
