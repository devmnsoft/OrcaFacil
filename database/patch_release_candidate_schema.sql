-- OrçaFácil RC: atualização exclusivamente aditiva para instalações existentes.
-- Pode ser executada repetidamente. Não remove tabelas, dados, usuários ou senhas.
\set ON_ERROR_STOP on
BEGIN;

CREATE SCHEMA IF NOT EXISTS orcafacil;

-- O script completo cria a tabela. Este patch falha de forma clara quando uma
-- instalação incompleta não possui users, evitando criar uma tabela parcial.
DO $rc$
BEGIN
  IF to_regclass('orcafacil.users') IS NULL THEN
    RAISE EXCEPTION 'Banco desatualizado: execute database/script_completop.sql antes deste patch.';
  END IF;
END $rc$;

ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS failed_login_attempts integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS last_failed_login_at timestamptz;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS last_successful_login_at timestamptz;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS locked_until timestamptz;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS is_blocked boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS block_reason varchar(500);
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS must_change_password boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_changed_at timestamptz;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_changed_by_user_id uuid;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_expires_at timestamptz;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_reset_reason varchar(500);
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS session_version integer NOT NULL DEFAULT 1;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS accepted_privacy_at timestamptz;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS accepted_terms_at timestamptz;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS legacy_unversioned_acceptance boolean NOT NULL DEFAULT false;

CREATE INDEX IF NOT EXISTS ix_users_locked_until
  ON orcafacil.users (locked_until) WHERE locked_until IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_users_blocked
  ON orcafacil.users (is_blocked) WHERE is_blocked = true;

COMMIT;

SELECT CASE WHEN count(*) = 15 THEN 'RC schema de autenticação atualizado'
            ELSE 'Banco desatualizado: execute o script de atualização antes de continuar.' END AS summary
FROM information_schema.columns
WHERE table_schema = 'orcafacil' AND table_name = 'users'
  AND column_name = ANY (ARRAY[
    'failed_login_attempts','last_failed_login_at','last_successful_login_at','locked_until','is_blocked',
    'block_reason','must_change_password','password_changed_at','password_changed_by_user_id','password_expires_at',
    'password_reset_reason','session_version','accepted_privacy_at','accepted_terms_at','legacy_unversioned_acceptance']);
