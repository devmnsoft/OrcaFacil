-- Reparo aditivo e idempotente para bancos locais anteriores às colunas de segurança.
-- Aplicação: psql -h localhost -p 5432 -U postgres -d orcafacil \
--   -f database/patch_fix_users_login_security_columns.sql
BEGIN;

ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS accepted_privacy_at timestamptz NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS accepted_terms_at timestamptz NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS block_reason varchar(500) NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS failed_login_attempts integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS is_blocked boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS last_failed_login_at timestamptz NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS last_successful_login_at timestamptz NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS legacy_unversioned_acceptance boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS locked_until timestamptz NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS must_change_password boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_changed_at timestamptz NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_changed_by_user_id uuid NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_expires_at timestamptz NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_reset_reason varchar(500) NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS session_version integer NOT NULL DEFAULT 1;

COMMIT;

-- Validação esperada: 15 linhas.
SELECT column_name
FROM information_schema.columns
WHERE table_schema = 'orcafacil'
  AND table_name = 'users'
  AND column_name IN (
    'failed_login_attempts', 'last_failed_login_at', 'last_successful_login_at',
    'locked_until', 'is_blocked', 'block_reason', 'must_change_password',
    'password_changed_at', 'password_changed_by_user_id', 'password_expires_at',
    'password_reset_reason', 'session_version', 'accepted_privacy_at',
    'accepted_terms_at', 'legacy_unversioned_acceptance'
  )
ORDER BY column_name;
