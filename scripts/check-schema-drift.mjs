import fs from 'node:fs';
const source=fs.readFileSync('src/OrcaFacil.Persistence/Diagnostics/DatabaseDiagnosticsService.cs','utf8');
for(const token of ['documents.template_code','documents.client_snapshot','documents.template_snapshot','documents.row_version','documents.follow_up_status','documents.next_follow_up_at','documents.public_token','documents.payment_method','documents.deposit_amount','budget_templates.account_id','budget_templates.user_id','budget_templates.is_system_template','account_onboarding_states.current_step','email_outbox_messages','hotfix_documents_full_schema_drift_v61.sql'])if(!source.includes(token))throw new Error(`diagnóstico sem ${token}`);
console.log('schema drift: OK');
