import { existsSync, readFileSync } from 'node:fs';
const sql=readFileSync('database/script_completop.sql','utf8').toLowerCase();
const requiredTables=['users','business_accounts','account_members','account_onboarding_states','clients','service_catalog_items','documents','document_items','public_quotes','work_orders','manual_payments','receipts','notifications','audit_logs','system_logs','email_outbox_messages','plans','plan_versions','features','plan_feature_values','subscriptions'];
const missing=requiredTables.filter(t=>!new RegExp(`create\\s+table\\s+if\\s+not\\s+exists\\s+orcafacil\\.${t}\\b`,'i').test(sql));
const requiredTokens=['create schema if not exists orcafacil','on conflict','failed_login_attempts','session_version','ix_account_onboarding_states_account_id_user_id'];
for(const token of requiredTokens)if(!sql.includes(token))missing.push(token);
if(!existsSync('database/seed-superadmin.sql'))missing.push('database/seed-superadmin.sql');
if(/drop\s+(table|schema|database)|truncate\s+/i.test(sql))missing.push('operação destrutiva detectada');
if(missing.length){console.error('Contrato V1 incompleto/inseguro:\n'+missing.join('\n'));process.exit(1)}
console.log(`Schema V1: ${requiredTables.length} tabelas obrigatórias, seeds idempotentes e política não destrutiva OK.`);
