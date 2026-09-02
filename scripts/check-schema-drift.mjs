import fs from 'node:fs';
const source=fs.readFileSync('src/OrcaFacil.Persistence/Diagnostics/DatabaseDiagnosticsService.cs','utf8');
for(const token of ['documents.client_snapshot','documents.template_snapshot','documents.follow_up_status','documents.next_follow_up_at','documents.public_token','account_onboarding_states.current_step','email_outbox_messages'])if(!source.includes(token))throw new Error(`diagnóstico sem ${token}`);
console.log('schema drift: OK');
