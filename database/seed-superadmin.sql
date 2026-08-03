\set ON_ERROR_STOP on
\if :{?superadmin_email}
\else
  \echo 'ERRO: informe -v superadmin_email.'
  \quit 2
\endif
\if :{?superadmin_password}
\else
  \echo 'ERRO: informe -v superadmin_password por canal seguro.'
  \quit 2
\endif
\if :{?environment}
\else
  \echo 'ERRO: informe -v environment.'
  \quit 2
\endif

SELECT lower(trim(:'superadmin_email')) AS normalized_email,
       lower(trim(:'environment')) = 'production' AS production,
       lower(trim(:'environment')) IN ('development','staging','homologation') AS environment_allowed,
       lower(trim(:'superadmin_email')) ~ '^[^[:space:]@]+@[^[:space:]@]+\.[^[:space:]@]+$' AS email_valid,
       length(:'superadmin_password') >= 14
         AND :'superadmin_password' ~ '[A-Z]'
         AND :'superadmin_password' ~ '[a-z]'
         AND :'superadmin_password' ~ '[0-9]'
         AND :'superadmin_password' ~ '[^[:alnum:]]' AS password_valid
\gset

\if :production
  \echo 'BLOQUEADO: credencial local conhecida nunca é criada em Production.'
  \quit 3
\endif
\if :environment_allowed
\else
  \echo 'ERRO: ambiente deve ser Development, Staging ou Homologation.'
  \quit 4
\endif
\if :email_valid
\else
  \echo 'ERRO: e-mail inválido.'
  \quit 5
\endif
\if :password_valid
\else
  \echo 'ERRO: senha não atende aos requisitos de complexidade.'
  \quit 6
\endif

BEGIN;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

WITH candidate AS (
  SELECT gen_random_uuid() AS id
  WHERE NOT EXISTS (SELECT 1 FROM orcafacil.users WHERE email = :'normalized_email')
), inserted AS (
  INSERT INTO orcafacil.users
    (id, name, email, password_hash, role, plan, is_active, is_blocked,
     must_change_password, failed_login_attempts, session_version, password_reset_reason,
     legacy_unversioned_acceptance, created_at, is_deleted)
  SELECT id, 'SuperAdmin', :'normalized_email',
         crypt(:'superadmin_password', gen_salt('bf', 12)),
         'SuperAdmin', 'Business', true, false, true, 0, 1, 'LocalBootstrap', false, now(), false
  FROM candidate
  RETURNING id
), audited AS (
  INSERT INTO orcafacil.audit_logs
    (id, user_id, action, entity_type, entity_id, metadata_json, created_at, is_deleted)
  SELECT gen_random_uuid(), id, 'SuperAdminLocalBootstrap', 'UserAccount', id::text,
         jsonb_build_object('environment', :'environment', 'status', 'created'), now(), false
  FROM inserted
)
SELECT 1;

-- Compatibilidade canônica sem substituir credenciais existentes.
UPDATE orcafacil.users
SET role = 'SuperAdmin', plan = 'Business', updated_at = now()
WHERE email = :'normalized_email' AND role IN ('SuperAdmin', 'SuperAdministrator');
COMMIT;

SELECT id,
       left(email, 2) || '***@' || split_part(email, '@', 2) AS masked_email,
       CASE WHEN must_change_password THEN 'troca_obrigatoria' ELSE 'existente' END AS status
FROM orcafacil.users WHERE email = :'normalized_email';
