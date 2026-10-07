-- V6.9: aplicação atômica de sugestões de IA, governança de permissões de IA,
-- benefício de IA por plano e reparo explícito dos índices de unicidade do V6.8.
-- Aditivo e idempotente. Não apaga dados de negócio válidos.
CREATE SCHEMA IF NOT EXISTS orcafacil;

-- ---------------------------------------------------------------------------
-- 1. Estado de aplicação da revisão de sugestão (fonte de verdade em colunas).
-- ---------------------------------------------------------------------------
ALTER TABLE orcafacil.ai_suggestion_cards ADD COLUMN IF NOT EXISTS status varchar(24) NOT NULL DEFAULT 'PendingReview';
ALTER TABLE orcafacil.ai_suggestion_cards ADD COLUMN IF NOT EXISTS applied_document_id uuid;
ALTER TABLE orcafacil.ai_suggestion_cards ADD COLUMN IF NOT EXISTS apply_fingerprint varchar(128);
ALTER TABLE orcafacil.ai_suggestion_cards ADD COLUMN IF NOT EXISTS applied_at timestamptz;

-- Backfill: cartões gravados antes da V6.9 guardavam o status dentro do JSON.
UPDATE orcafacil.ai_suggestion_cards
SET status = COALESCE(NULLIF(data_json ->> 'Status', ''), NULLIF(data_json ->> 'status', ''), 'PendingReview')
WHERE status = 'PendingReview'
  AND COALESCE(NULLIF(data_json ->> 'Status', ''), NULLIF(data_json ->> 'status', '')) <> '';

ALTER TABLE orcafacil.ai_suggestion_cards
    DROP CONSTRAINT IF EXISTS ck_ai_suggestion_cards_status;
ALTER TABLE orcafacil.ai_suggestion_cards
    ADD CONSTRAINT ck_ai_suggestion_cards_status CHECK (status IN ('PendingReview', 'Applied', 'Dismissed'));

CREATE INDEX IF NOT EXISTS ix_ai_suggestion_cards_pending
    ON orcafacil.ai_suggestion_cards (account_id, created_at DESC)
    WHERE status = 'PendingReview' AND is_deleted = false;

-- ---------------------------------------------------------------------------
-- 2. Reparo explícito dos índices de unicidade que a V6.8 podia omitir em
--    silêncio quando havia duplicidades. Diagnóstico prévio, recuperação sem
--    apagar dados válidos e pós-condição obrigatória.
-- ---------------------------------------------------------------------------

-- 2a. ai_usage_logs: duplicidades de (account_id, operation_type, correlation_id)
--     são registros redundantes da MESMA operação (mesma reserva/correlação).
--     A recuperação preserva o registro mais antigo e remove apenas cópias
--     redundantes da mesma operação; nenhuma operação distinta é apagada.
DO $$
DECLARE
    duplicated integer;
BEGIN
    SELECT count(*) INTO duplicated
    FROM (
        SELECT 1
        FROM orcafacil.ai_usage_logs
        GROUP BY account_id, operation_type, correlation_id
        HAVING count(*) > 1
    ) d;

    IF duplicated > 0 THEN
        RAISE NOTICE 'V6.9: % grupo(s) duplicado(s) em ai_usage_logs; removendo apenas cópias redundantes da mesma operação (o registro mais antigo é preservado).', duplicated;
        DELETE FROM orcafacil.ai_usage_logs a
        USING (
            SELECT id,
                   row_number() OVER (
                       PARTITION BY account_id, operation_type, correlation_id
                       ORDER BY created_at ASC, id ASC
                   ) AS rn
            FROM orcafacil.ai_usage_logs
        ) ranked
        WHERE a.id = ranked.id AND ranked.rn > 1;
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_usage_operation
    ON orcafacil.ai_usage_logs (account_id, operation_type, correlation_id);

-- 2b. documents.last_autosave_key: duplicidades aqui são rascunhos reais e
--     NÃO são apagados. O upgrade falha com diagnóstico explícito para que o
--     operador reatribua as chaves antes de garantir a unicidade.
DO $$
DECLARE
    duplicated integer;
BEGIN
    SELECT count(*) INTO duplicated
    FROM (
        SELECT 1
        FROM orcafacil.documents
        WHERE last_autosave_key IS NOT NULL AND is_deleted = false
        GROUP BY account_id, last_autosave_key
        HAVING count(*) > 1
    ) d;

    IF duplicated > 0 THEN
        RAISE EXCEPTION 'V6.9: % grupo(s) de rascunhos compartilham a mesma chave de salvamento (last_autosave_key). Nenhum rascunho foi apagado. Reatribua as chaves duplicadas (ex.: defina last_autosave_key = NULL nos rascunhos duplicados mais recentes) e execute a migration novamente.', duplicated;
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_documents_account_autosave_key
    ON orcafacil.documents (account_id, last_autosave_key)
    WHERE last_autosave_key IS NOT NULL AND is_deleted = false;

-- 2c. Pós-condição: o upgrade não pode terminar sem as garantias de unicidade.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'orcafacil' AND indexname = 'ux_ai_usage_operation') THEN
        RAISE EXCEPTION 'V6.9: pós-condição falhou — o índice ux_ai_usage_operation não existe ao final do upgrade.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'orcafacil' AND indexname = 'ux_documents_account_autosave_key') THEN
        RAISE EXCEPTION 'V6.9: pós-condição falhou — o índice ux_documents_account_autosave_key não existe ao final do upgrade.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'orcafacil' AND indexname = 'ux_ai_quota_reservations_operation') THEN
        RAISE EXCEPTION 'V6.9: pós-condição falhou — o índice ux_ai_quota_reservations_operation não existe ao final do upgrade (verifique duplicidades em ai_quota_reservations).';
    END IF;
END $$;

-- ---------------------------------------------------------------------------
-- 3. Permissões canônicas de IA: seed e concessão por perfil de conta.
--    Owner/Administrator/Manager recebem todas; Commercial pode gerar e aplicar
--    sugestões; demais perfis não recebem acesso à IA.
-- ---------------------------------------------------------------------------
INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission,created_at,is_deleted) VALUES
 ('Ai.View','Visualizar recursos de IA',false,now(),false),
 ('Ai.GenerateDrafts','Gerar sugestões e rascunhos de IA',false,now(),false),
 ('Ai.ApplySuggestions','Aplicar sugestões de IA revisadas',false,now(),false),
 ('Ai.ViewUsage','Visualizar consumo de IA da conta',false,now(),false)
ON CONFLICT(code) DO UPDATE SET display_name=EXCLUDED.display_name,is_deleted=false;

INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted)
SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p
WHERE (
    (r.code IN ('Owner','Administrator','Manager') AND p.code IN ('Ai.View','Ai.GenerateDrafts','Ai.ApplySuggestions','Ai.ViewUsage'))
    OR (r.code = 'Commercial' AND p.code IN ('Ai.View','Ai.GenerateDrafts','Ai.ApplySuggestions'))
)
ON CONFLICT(role_id,permission_id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- 4. Benefício explícito de IA por plano: provedores externos e limite mensal.
--    FREE: somente regras internas (sem provedor externo). Planos pagos: IA
--    externa com limite mensal por conta. ENTERPRISE: ilimitado.
--    Nenhum contrato existente é alterado: valores são apenas inseridos quando
--    ausentes (ON CONFLICT DO NOTHING).
-- ---------------------------------------------------------------------------
INSERT INTO orcafacil.features(id,code,display_name,description,value_type,category)
SELECT md5(code)::uuid,code,name,descr,type,'Inteligência' FROM (VALUES
 ('ai.external.enabled','IA com provedores externos','Permite que a conta use Groq, Gemini ou DeepSeek configurados; sem o benefício, a IA opera somente por regras internas.','Boolean'),
 ('ai.monthly_limit','Operações de IA por mês','Limite mensal de operações de IA externa da conta.','Integer')
) f(code,name,descr,type) ON CONFLICT(code) DO NOTHING;

INSERT INTO orcafacil.plan_feature_values(id,plan_version_id,feature_id,boolean_value,integer_value,is_unlimited,created_at,is_deleted)
SELECT gen_random_uuid(), pv.id, f.id, v.enabled, v.limit_value, v.unlimited, now(), false
FROM orcafacil.plan_versions pv
JOIN orcafacil.plans p ON p.id = pv.plan_id
JOIN orcafacil.features f ON f.code = 'ai.external.enabled'
JOIN (VALUES
    ('FREE', false, NULL::integer, false),
    ('PROFESSIONAL', true, NULL::integer, false),
    ('BUSINESS', true, NULL::integer, false),
    ('ENTERPRISE', true, NULL::integer, false)
) v(plan_code, enabled, limit_value, unlimited) ON v.plan_code = p.code
WHERE pv.status = 'Published' AND pv.is_deleted = false AND p.is_deleted = false
ON CONFLICT(plan_version_id,feature_id) DO NOTHING;

INSERT INTO orcafacil.plan_feature_values(id,plan_version_id,feature_id,boolean_value,integer_value,is_unlimited,created_at,is_deleted)
SELECT gen_random_uuid(), pv.id, f.id, true, v.limit_value, v.unlimited, now(), false
FROM orcafacil.plan_versions pv
JOIN orcafacil.plans p ON p.id = pv.plan_id
JOIN orcafacil.features f ON f.code = 'ai.monthly_limit'
JOIN (VALUES
    ('FREE', 10, false),
    ('PROFESSIONAL', 200, false),
    ('BUSINESS', 1000, false),
    ('ENTERPRISE', NULL::integer, true)
) v(plan_code, limit_value, unlimited) ON v.plan_code = p.code
WHERE pv.status = 'Published' AND pv.is_deleted = false AND p.is_deleted = false
ON CONFLICT(plan_version_id,feature_id) DO NOTHING;
