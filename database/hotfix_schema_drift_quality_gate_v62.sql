-- V6.1 P0: reconcile the complete EF Document contract without dropping data.
-- Document.RowVersion is a C# byte[] application-managed concurrency token, hence bytea (not xmin/bigint).
BEGIN;

ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_code varchar(40);
UPDATE orcafacil.documents SET template_code = 'essential' WHERE template_code IS NULL OR btrim(template_code) = '';
ALTER TABLE orcafacil.documents ALTER COLUMN template_code SET DEFAULT 'essential';
ALTER TABLE orcafacil.documents ALTER COLUMN template_code SET NOT NULL;

ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS row_version bytea;
UPDATE orcafacil.documents SET row_version = decode(replace(gen_random_uuid()::text, '-', ''), 'hex') WHERE row_version IS NULL;
ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET DEFAULT decode(replace(gen_random_uuid()::text, '-', ''), 'hex');
ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET NOT NULL;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS conditions_text text;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS payment_method varchar(60);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS pix_information varchar(300);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS deposit_amount numeric(18,2);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS installment_count integer;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS estimated_duration varchar(120);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS expected_start_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS evidence_hash varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_status varchar(24) NOT NULL DEFAULT 'None';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS next_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS current_wizard_step integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosave_key varchar(80);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosaved_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_token varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision varchar(40) NOT NULL DEFAULT 'Pending';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS internal_approval_status varchar(24);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS requires_internal_approval boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS assigned_team_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS assigned_to_user_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS business_unit_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_city varchar(120);

-- Refuse to conceal incompatible existing types; ADD COLUMN IF NOT EXISTS alone cannot repair those safely.
DO $validation$
DECLARE mismatch text;
BEGIN
  SELECT string_agg(expected.column_name || ' expected ' || expected.expected_type || ', found ' || coalesce(actual.data_type, '<missing>'), '; ')
    INTO mismatch
    FROM (VALUES
      ('template_code','character varying'), ('template_snapshot','jsonb'), ('row_version','bytea'),
      ('client_snapshot','jsonb'), ('conditions_text','text'), ('payment_method','character varying'),
      ('deposit_amount','numeric'), ('current_wizard_step','integer'), ('public_enabled','boolean')
    ) AS expected(column_name,expected_type)
    LEFT JOIN information_schema.columns actual
      ON actual.table_schema='orcafacil' AND actual.table_name='documents' AND actual.column_name=expected.column_name
   WHERE actual.data_type IS DISTINCT FROM expected.expected_type;
  IF mismatch IS NOT NULL THEN RAISE EXCEPTION 'documents schema differs from EF contract: %', mismatch; END IF;
END $validation$;

CREATE INDEX IF NOT EXISTS ix_documents_account_type_created ON orcafacil.documents(account_id, type, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_followup ON orcafacil.documents(account_id, type, next_follow_up_at);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_valid_until ON orcafacil.documents(account_id, type, valid_until);
CREATE INDEX IF NOT EXISTS ix_documents_public_token ON orcafacil.documents(public_token) WHERE public_token IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_documents_template_code ON orcafacil.documents(account_id, template_code) WHERE template_code IS NOT NULL AND is_deleted = false;

COMMIT;

-- V6.2 consolidates the complete critical contract without destructive operations.
BEGIN;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS title varchar(160);
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS profession varchar(120);
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
CREATE INDEX IF NOT EXISTS ix_budget_templates_account_active ON orcafacil.budget_templates(account_id, is_active) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_budget_template_items_template ON orcafacil.budget_template_items(template_id);
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS is_system_template boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS updated_at timestamptz;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS deleted_by uuid;
COMMIT;
