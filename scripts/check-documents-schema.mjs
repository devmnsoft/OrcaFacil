import fs from 'node:fs';
const files=['database/script_completop.sql','database/patch_release_candidate_schema.sql'];
const required=['client_snapshot','template_snapshot','follow_up_status','next_follow_up_at','public_token','client_decision','internal_approval_status','ix_documents_account_type_followup','ix_documents_account_type_valid_until','ix_documents_public_token'];
for(const file of files){const sql=fs.readFileSync(file,'utf8').toLowerCase();for(const token of required)if(!sql.includes(token))throw new Error(`${file}: ausente ${token}`);if(!sql.includes('add column if not exists'))throw new Error(`${file}: patch não idempotente`);}
console.log('documents schema: OK');
