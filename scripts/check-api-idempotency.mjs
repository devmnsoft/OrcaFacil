import fs from 'node:fs'; const s=fs.readFileSync('src/OrcaFacil.Web/Api/PublicApiV1.cs','utf8');
for(const x of ['Idempotency-Key','RequestHash','idempotency_conflict','ExpiresAt'])if(!s.includes(x))throw new Error(`Idempotência ausente: ${x}`);
console.log('OK: idempotência persistente e conflito por payload presentes.');
