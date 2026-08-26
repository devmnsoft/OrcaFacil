import fs from 'node:fs';
const text = fs.readFileSync('src/OrcaFacil.Web/Email/EmailOutboxWorker.cs', 'utf8');
for (const token of ['databaseConfiguration.IsValid', 'BackoffFor', 'TimeSpan.FromMinutes(5)', 'EMAIL_OUTBOX_WORKER_RECOVERED'])
  if (!text.includes(token)) throw new Error(`Worker resilience ausente: ${token}`);
console.log('Email outbox worker resilience: OK');
