import { read, requireAll, forbid, ok } from './schema-drift-check-utils.mjs';
const hotfix=read('database/hotfix_commercial_schema_drift_v57.sql');
requireAll(hotfix,['BEGIN;','COMMIT;','ADD COLUMN IF NOT EXISTS payment_method','ADD COLUMN IF NOT EXISTS account_id','ix_budget_templates_account_active'], 'hotfix V5.7');
forbid(hotfix,[/^\s*DROP\s/im,/^\s*DELETE\s+FROM\b/im,/^\s*TRUNCATE\b/im], 'hotfix V5.7');
requireAll(read('src/OrcaFacil.Persistence/Migrations/20260903000000_FixCommercialSchemaDriftBudgetTemplatesAndDocumentsV57.cs'),['payment_method','budget_templates','account_id'], 'migration V5.7');
ok('hotfix comercial V5.7 idempotente e não destrutivo');
