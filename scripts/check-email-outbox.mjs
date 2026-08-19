import { readFile } from 'node:fs/promises';

const [worker, model, page] = await Promise.all([
  readFile('src/OrcaFacil.Web/Email/EmailOutboxWorker.cs', 'utf8'),
  readFile('src/OrcaFacil.Domain/Entities/EmailOutboxMessage.cs', 'utf8'),
  readFile('src/OrcaFacil.Web/Areas/Admin/Pages/EmailOutbox/Index.cshtml.cs', 'utf8')
]);
for (const marker of ['Attempts','NextAttemptAt','Status']) if (!model.includes(marker)) throw new Error(`Outbox sem ${marker}.`);
if (!worker.includes('BackgroundService')) throw new Error('Worker da outbox não está ativo.');
if (!page.includes('SuperAdminOnly') || !page.includes('EmailOutbox.Retry')) throw new Error('Reprocessamento não está protegido/auditado.');
if (/Smtp.*Password|Password.*Smtp/i.test(page)) throw new Error('Tela da outbox referencia segredo SMTP.');
console.log('Fila, worker e reprocessamento protegido da EmailOutbox validados.');
