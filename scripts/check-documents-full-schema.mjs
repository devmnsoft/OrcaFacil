import { readFileSync } from 'node:fs';
const sql = readFileSync('database/hotfix_documents_full_schema_drift_v61.sql','utf8').toLowerCase();
const columns = ['template_code','template_snapshot','row_version','client_snapshot','conditions_text','payment_method','pix_information','deposit_amount','installment_count','estimated_duration','expected_start_at','warranty_text','evidence_hash','follow_up_status','follow_up_note','last_follow_up_at','next_follow_up_at','current_wizard_step','last_autosave_key','last_autosaved_at','public_enabled','public_token','client_decision','client_decision_at','client_decision_note','internal_approval_status','requires_internal_approval','converted_receipt_id','converted_receipt_number','origin_budget_id','origin_budget_number','assigned_team_id','assigned_to_user_id','business_unit_id','client_city'];
for (const column of columns) if (!sql.includes(`add column if not exists ${column}`)) throw new Error(`hotfix sem documents.${column}`);
for (const token of ['row_version bytea','template_snapshot jsonb','client_snapshot jsonb','deposit_amount numeric(18,2)','do $validation$','begin;','commit;']) if (!sql.includes(token)) throw new Error(`hotfix sem ${token}`);
if (/\bdrop\b/.test(sql)) throw new Error('hotfix destrutivo');
console.log(`documents full schema: OK (${columns.length} colunas)`);
