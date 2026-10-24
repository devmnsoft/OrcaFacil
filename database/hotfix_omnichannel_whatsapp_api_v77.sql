-- OrçaFácil V7.7: conta da Cloud API do WhatsApp (Meta) em orcafacil.omnichannel_whatsapp_accounts.
-- Aditivo e idempotente. A tabela genérica é criada pelo sprint38 (encadeado na base nova);
-- aqui entram as colunas tipadas da integração oficial. Não remove nem reescreve dados.
BEGIN;

ALTER TABLE orcafacil.omnichannel_whatsapp_accounts
    ADD COLUMN IF NOT EXISTS phone_number_id varchar(100),
    ADD COLUMN IF NOT EXISTS business_account_id varchar(100),
    ADD COLUMN IF NOT EXISTS display_name varchar(200),
    ADD COLUMN IF NOT EXISTS protected_access_token text,
    ADD COLUMN IF NOT EXISTS protected_app_secret text,
    ADD COLUMN IF NOT EXISTS webhook_verify_token varchar(200),
    ADD COLUMN IF NOT EXISTS is_enabled boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS status varchar(20) NOT NULL DEFAULT 'NotConfigured',
    ADD COLUMN IF NOT EXISTS last_health_check_at timestamptz,
    ADD COLUMN IF NOT EXISTS last_failure varchar(1000);

-- Uma conta WhatsApp por conta de negócio é garantida em código (upsert por account_id);
-- o índice genérico account_id já existe no sprint38.

COMMIT;
