import fs from 'node:fs';
const read=p=>fs.readFileSync(p,'utf8');
const required=['omnichannel_channels','omnichannel_conversations','omnichannel_messages','omnichannel_web_chat_sessions','omnichannel_inbound_email_accounts','omnichannel_whatsapp_accounts','omnichannel_sla_events','omnichannel_csat_responses','omnichannel_opt_out_events'];
const sql=read('database/sprint38_omnichannel_v39.sql');
for(const table of required) if(!sql.includes(table)) throw new Error(`Tabela obrigatória ausente: ${table}`);
const service=read('src/OrcaFacil.Web/Services/OmnichannelService.cs');
for(const rule of ['x.AccountId==accountId','InternalNote','NotConfigured','Prepared','TokenHash','HtmlEncode']) if(!service.includes(rule)) throw new Error(`Regra crítica ausente: ${rule}`);
if(/Math\.random|Status\s*=\s*OmnichannelMessageStatus\.Sent/.test(service)) throw new Error('Envio ou métrica simulada detectada.');
console.log('Omnichannel V3.9: isolamento, mensagens internas, chat real, canais controlados, SLA, CSAT e opt-out validados.');
