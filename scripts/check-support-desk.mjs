import fs from 'node:fs';
const required={
  'src/OrcaFacil.Domain/Entities/SupportDesk.cs':['SupportQueue','SupportQueueMember','SupportSlaPolicy','SupportIncident','SupportCsatSurvey'],
  'src/OrcaFacil.Web/Services/SupportDeskService.cs':['PublicMessagesAsync','AssignAsync','EscalateAsync','ApplySlaAsync','RecordBreachOnceAsync','CreateCsatSurveyAsync','RespondCsatAsync'],
  'database/sprint37_support_desk_v38.sql':['CREATE TABLE IF NOT EXISTS','support_queues','support_ticket_sla_events','support_incidents','support_ticket_csat_responses']
};
const errors=[]; for(const [file,tokens] of Object.entries(required)){if(!fs.existsSync(file)){errors.push(`ausente: ${file}`);continue;}const text=fs.readFileSync(file,'utf8');for(const token of tokens)if(!text.includes(token))errors.push(`${file}: contrato ausente ${token}`)}
if(errors.length){console.error(errors.join('\n'));process.exit(1)} console.log('Service Desk V3.8: contratos críticos presentes.');
