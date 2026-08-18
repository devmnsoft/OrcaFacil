import fs from 'node:fs';
const required=['src/OrcaFacil.Web/Pages/Support/Tickets.cshtml','src/OrcaFacil.Web/Pages/Support/TicketDetails.cshtml','src/OrcaFacil.Web/Areas/Admin/Pages/Support/Index.cshtml','src/OrcaFacil.Web/Areas/Admin/Pages/Support/Details.cshtml','src/OrcaFacil.Web/Pages/Feedback/Index.cshtml','src/OrcaFacil.Web/wwwroot/js/ui/feedback-widget.js'];
const missing=required.filter(x=>!fs.existsSync(x));if(missing.length)throw new Error(`Arquivos ausentes: ${missing.join(', ')}`);
const pages=required.filter(x=>x.endsWith('.cshtml')).map(x=>fs.readFileSync(x,'utf8')).join('\n');
for(const bad of ['href="#"','href=""','javascript:void'])if(pages.includes(bad))throw new Error(`Ação falsa detectada: ${bad}`);
const ticket=fs.readFileSync('src/OrcaFacil.Domain/Entities/SupportTicket.cs','utf8');for(const field of ['AccountId','RelatedPage','CorrelationId','BrowserInfo','ClosedAt'])if(!ticket.includes(field))throw new Error(`Campo obrigatório ausente: ${field}`);
console.log('Support readiness: rotas, contexto seguro e ações reais verificados.');
