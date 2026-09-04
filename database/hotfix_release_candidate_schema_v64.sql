-- OrçaFácil V6.4: gate aditivo de prontidão. Não remove nem reescreve dados.
\set ON_ERROR_STOP on
BEGIN;
CREATE SCHEMA IF NOT EXISTS orcafacil;

DO $v64$
DECLARE missing text;
BEGIN
  SELECT string_agg(required.name, ', ' ORDER BY required.name) INTO missing
  FROM unnest(ARRAY[
    'users','account_members','business_accounts','issuer_profiles','clients','contacts',
    'service_catalog_items','documents','document_items','document_revisions','budget_templates',
    'budget_template_items','audit_logs','account_onboarding_states','email_outbox_messages',
    'plans','plan_versions','features','plan_feature_values','subscriptions','notifications',
    'work_orders','payments','receipts','manual_payments','public_document_decisions'
  ]) required(name)
  WHERE NOT EXISTS (SELECT 1 FROM information_schema.tables t
    WHERE t.table_schema='orcafacil' AND t.table_name=required.name);
  IF missing IS NOT NULL THEN
    RAISE EXCEPTION 'Schema V6.4 incompleto. Tabelas ausentes: %. Execute database/script_completop.sql.', missing;
  END IF;
END $v64$;
CREATE INDEX IF NOT EXISTS ix_budget_templates_account_active
  ON orcafacil.budget_templates (account_id, is_active)
  WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_budget_templates_user_active
  ON orcafacil.budget_templates (user_id, is_active)
  WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_public_document_decisions_account_document
  ON orcafacil.public_document_decisions (account_id, document_id, created_at DESC)
  WHERE is_deleted = false;

COMMIT;
