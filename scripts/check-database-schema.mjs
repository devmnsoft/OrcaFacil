import { existsSync, readFileSync } from 'node:fs';
const files=['database/script_completop.sql','database/patch_release_candidate_schema.sql','database/patch_fix_account_onboarding_states.sql','database/seed-superadmin.sql'];
const missingFiles=files.filter(file=>!existsSync(file));
if(missingFiles.length){console.error('Scripts SQL ausentes:\n'+missingFiles.join('\n'));process.exit(1)}
const contents=Object.fromEntries(files.map(file=>[file,readFileSync(file,'utf8')]));
const sql=contents[files[0]].toLowerCase();
const requiredTables=['users','business_accounts','account_members','account_onboarding_states','clients','service_catalog_items','documents','document_items','public_quotes','work_orders','manual_payments','receipts','notifications','audit_logs','system_logs','email_outbox_messages','plans','plan_versions','features','plan_feature_values','subscriptions'];
const missing=requiredTables.filter(t=>!new RegExp(`create\\s+table\\s+if\\s+not\\s+exists\\s+orcafacil\\.${t}\\b`,'i').test(sql));
const requiredTokens=['create schema if not exists orcafacil','on conflict','failed_login_attempts','session_version','ix_account_onboarding_states_account_id_user_id'];
for(const token of requiredTokens)if(!sql.includes(token))missing.push(token);
for(const [file,content] of Object.entries(contents)) if(/\bdrop\s+(table|schema|database)\b|\btruncate\s+/i.test(content)) missing.push(`${file}: operação destrutiva detectada`);
const releasePatch=contents[files[1]];
for(const token of ['ADD COLUMN IF NOT EXISTS failed_login_attempts','ADD COLUMN IF NOT EXISTS session_version','CREATE TABLE IF NOT EXISTS orcafacil.account_onboarding_states']) if(!releasePatch.includes(token)) missing.push(`${files[1]}: ${token}`);
const onboardingPatch=contents[files[2]];
for(const token of ['CREATE TABLE IF NOT EXISTS orcafacil.account_onboarding_states','CREATE UNIQUE INDEX IF NOT EXISTS ix_account_onboarding_states_account_id_user_id']) if(!onboardingPatch.includes(token)) missing.push(`${files[2]}: ${token}`);
const seed=contents[files[3]];
for(const token of ['WHERE NOT EXISTS','BEGIN;','COMMIT;','must_change_password']) if(!seed.includes(token)) missing.push(`${files[3]}: ${token}`);
if(missing.length){console.error('Contrato V1 incompleto/inseguro:\n'+missing.join('\n'));process.exit(1)}
console.log(`Schema V1: ${requiredTables.length} tabelas e ${files.length} scripts obrigatórios, seed idempotente e política não destrutiva OK.`);
