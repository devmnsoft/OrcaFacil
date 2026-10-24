-- hotfix_plan_feature_catalog_v75.sql
-- FECHÔ 2.0 — Catálogo de planos coerente (idempotente e reversível).
--
-- Problema: a carga inicial (script_completop.sql) criava as tabelas features /
-- plan_feature_values, mas não provisionava a matriz de recursos por plano. Como o
-- gate de plano (PlanAccessService.CanUseAsync) lê essa matriz, recursos anunciados
-- como Aprovação pública, Ordens de serviço e Pagamentos manuais eram negados para
-- TODOS os planos, e o catálogo público (Precos) exibia os cards quase vazios.
-- Também faltavam os recursos work_orders.enabled, payments.manual.enabled e
-- receipts.operational.enabled, exigidos por código.
--
-- Este patch:
--   1. garante as linhas de features para todos os códigos de PlanFeatureCodes;
--   2. corrige o nome exibido de pdf.watermark (marca do produto: Fechô);
--   3. provisiona a matriz de plan_feature_values (FREE/PROFESSIONAL/BUSINESS/ENTERPRISE)
--      conforme PlanCatalogDefinitions, preservando valores já existentes por plano.
--
-- Sem DROP/TRUNCATE. Reexecutável. Aplicar com: psql -f hotfix_plan_feature_catalog_v75.sql

BEGIN;

-- 1) Catálogo de recursos (features)
INSERT INTO orcafacil.features (code, display_name, description, value_type, category)
VALUES
    ('team.members_limit','Pessoas da equipe','Pessoas da equipe','Integer','Equipe'),
    ('clients.active_limit','Clientes ativos','Clientes ativos','Integer','Clientes'),
    ('services.active_limit','Serviços ativos','Serviços ativos','Integer','Serviços'),
    ('documents.monthly_limit','Documentos mensais','Documentos mensais','Integer','Documentos'),
    ('pdf.monthly_limit','PDFs mensais','PDFs mensais','Integer','Documentos'),
    ('pdf.watermark','Marca Fechô','Marca Fechô','Boolean','Documentos'),
    ('history.days_visible','Histórico visível','Histórico visível','Integer','Histórico'),
    ('templates.basic_limit','Modelos básicos','Modelos básicos','Integer','Modelos'),
    ('templates.custom_enabled','Modelos personalizados','Modelos personalizados','Boolean','Modelos'),
    ('branding.custom_logo','Logo próprio','Logo próprio','Boolean','Marca'),
    ('numbering.custom_prefix','Prefixo personalizado','Prefixo personalizado','Boolean','Documentos'),
    ('public_approval.enabled','Aprovação pública','Aprovação pública','Boolean','Documentos'),
    ('public_approval.monthly_limit','Aprovações mensais','Aprovações mensais','Integer','Documentos'),
    ('sharing.whatsapp','Compartilhamento por WhatsApp','Compartilhamento por WhatsApp','Boolean','Compartilhamento'),
    ('sharing.public_link','Link público','Link público','Boolean','Compartilhamento'),
    ('work_orders.enabled','Ordens de serviço','Ordens de serviço','Boolean','Comercial'),
    ('payments.manual.enabled','Pagamentos manuais','Pagamentos manuais','Boolean','Financeiro'),
    ('receipts.operational.enabled','Recibos operacionais','Recibos operacionais','Boolean','Documentos'),
    ('document.convert_to_receipt','Conversão em recibo','Conversão em recibo','Boolean','Documentos'),
    ('reports.basic','Relatórios básicos','Relatórios básicos','Boolean','Relatórios'),
    ('reports.advanced','Relatórios avançados','Relatórios avançados','Boolean','Relatórios'),
    ('commercial.pipeline','Pipeline comercial','Pipeline comercial','Boolean','Comercial'),
    ('commercial.followups','Acompanhamentos','Acompanhamentos','Boolean','Comercial'),
    ('commercial.metrics','Indicadores comerciais','Indicadores comerciais','Boolean','Comercial'),
    ('exports.csv','Exportação CSV','Exportação CSV','Boolean','Relatórios'),
    ('audit.account','Auditoria da conta','Auditoria da conta','Boolean','Auditoria'),
    ('support.priority','Suporte prioritário','Suporte prioritário','Boolean','Suporte'),
    ('ai.external.enabled','IA com provedores externos','IA com provedores externos','Boolean','Inteligência'),
    ('ai.monthly_limit','Operações de IA por mês','Operações de IA por mês','Integer','Inteligência')
ON CONFLICT (code) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    description  = EXCLUDED.description,
    value_type   = EXCLUDED.value_type,
    category     = EXCLUDED.category,
    is_active    = true,
    is_deleted   = false,
    updated_at   = now();

-- 2) Matriz de valores por plano (versão 1, publicada pela carga inicial).
--    Cobre apenas versões que existem; planos sem versão são ignorados.
INSERT INTO orcafacil.plan_feature_values (id, plan_version_id, feature_id, boolean_value, integer_value, is_unlimited, created_at, is_deleted)
SELECT gen_random_uuid(), pv.id, f.id, v.boolean_value, v.integer_value, v.is_unlimited, now(), false
FROM (VALUES
    ('FREE','team.members_limit',true,1,false),
    ('FREE','clients.active_limit',true,25,false),
    ('FREE','services.active_limit',true,20,false),
    ('FREE','documents.monthly_limit',true,20,false),
    ('FREE','pdf.monthly_limit',true,10,false),
    ('FREE','pdf.watermark',true,NULL,false),
    ('FREE','history.days_visible',true,90,false),
    ('FREE','templates.basic_limit',true,3,false),
    ('FREE','templates.custom_enabled',false,NULL,false),
    ('FREE','branding.custom_logo',false,NULL,false),
    ('FREE','numbering.custom_prefix',false,NULL,false),
    ('FREE','public_approval.enabled',false,NULL,false),
    ('FREE','public_approval.monthly_limit',false,NULL,false),
    ('FREE','sharing.whatsapp',false,NULL,false),
    ('FREE','sharing.public_link',false,NULL,false),
    ('FREE','work_orders.enabled',false,NULL,false),
    ('FREE','payments.manual.enabled',false,NULL,false),
    ('FREE','receipts.operational.enabled',true,NULL,false),
    ('FREE','document.convert_to_receipt',true,NULL,false),
    ('FREE','reports.basic',false,NULL,false),
    ('FREE','reports.advanced',false,NULL,false),
    ('FREE','commercial.pipeline',false,NULL,false),
    ('FREE','commercial.followups',false,NULL,false),
    ('FREE','commercial.metrics',false,NULL,false),
    ('FREE','exports.csv',false,NULL,false),
    ('FREE','audit.account',false,NULL,false),
    ('FREE','support.priority',false,NULL,false),
    ('FREE','ai.external.enabled',false,NULL,false),
    ('FREE','ai.monthly_limit',true,10,false),
    ('PROFESSIONAL','team.members_limit',true,1,false),
    ('PROFESSIONAL','clients.active_limit',true,NULL,true),
    ('PROFESSIONAL','services.active_limit',true,NULL,true),
    ('PROFESSIONAL','documents.monthly_limit',true,NULL,true),
    ('PROFESSIONAL','pdf.monthly_limit',true,NULL,true),
    ('PROFESSIONAL','pdf.watermark',false,NULL,false),
    ('PROFESSIONAL','history.days_visible',true,NULL,true),
    ('PROFESSIONAL','templates.basic_limit',true,NULL,true),
    ('PROFESSIONAL','templates.custom_enabled',true,NULL,false),
    ('PROFESSIONAL','branding.custom_logo',true,NULL,false),
    ('PROFESSIONAL','numbering.custom_prefix',true,NULL,false),
    ('PROFESSIONAL','public_approval.enabled',true,NULL,false),
    ('PROFESSIONAL','public_approval.monthly_limit',true,NULL,true),
    ('PROFESSIONAL','sharing.whatsapp',true,NULL,false),
    ('PROFESSIONAL','sharing.public_link',true,NULL,false),
    ('PROFESSIONAL','work_orders.enabled',true,NULL,false),
    ('PROFESSIONAL','payments.manual.enabled',true,NULL,false),
    ('PROFESSIONAL','receipts.operational.enabled',true,NULL,false),
    ('PROFESSIONAL','document.convert_to_receipt',true,NULL,false),
    ('PROFESSIONAL','reports.basic',true,NULL,false),
    ('PROFESSIONAL','reports.advanced',false,NULL,false),
    ('PROFESSIONAL','commercial.pipeline',false,NULL,false),
    ('PROFESSIONAL','commercial.followups',false,NULL,false),
    ('PROFESSIONAL','commercial.metrics',false,NULL,false),
    ('PROFESSIONAL','exports.csv',false,NULL,false),
    ('PROFESSIONAL','audit.account',false,NULL,false),
    ('PROFESSIONAL','support.priority',false,NULL,false),
    ('PROFESSIONAL','ai.external.enabled',true,NULL,false),
    ('PROFESSIONAL','ai.monthly_limit',true,200,false),
    ('BUSINESS','team.members_limit',true,3,false),
    ('BUSINESS','clients.active_limit',true,NULL,true),
    ('BUSINESS','services.active_limit',true,NULL,true),
    ('BUSINESS','documents.monthly_limit',true,NULL,true),
    ('BUSINESS','pdf.monthly_limit',true,NULL,true),
    ('BUSINESS','pdf.watermark',false,NULL,false),
    ('BUSINESS','history.days_visible',true,NULL,true),
    ('BUSINESS','templates.basic_limit',true,NULL,true),
    ('BUSINESS','templates.custom_enabled',true,NULL,false),
    ('BUSINESS','branding.custom_logo',true,NULL,false),
    ('BUSINESS','numbering.custom_prefix',true,NULL,false),
    ('BUSINESS','public_approval.enabled',true,NULL,false),
    ('BUSINESS','public_approval.monthly_limit',true,NULL,true),
    ('BUSINESS','sharing.whatsapp',true,NULL,false),
    ('BUSINESS','sharing.public_link',true,NULL,false),
    ('BUSINESS','work_orders.enabled',true,NULL,false),
    ('BUSINESS','payments.manual.enabled',true,NULL,false),
    ('BUSINESS','receipts.operational.enabled',true,NULL,false),
    ('BUSINESS','document.convert_to_receipt',true,NULL,false),
    ('BUSINESS','reports.basic',true,NULL,false),
    ('BUSINESS','reports.advanced',true,NULL,false),
    ('BUSINESS','commercial.pipeline',true,NULL,false),
    ('BUSINESS','commercial.followups',true,NULL,false),
    ('BUSINESS','commercial.metrics',true,NULL,false),
    ('BUSINESS','exports.csv',true,NULL,false),
    ('BUSINESS','audit.account',true,NULL,false),
    ('BUSINESS','support.priority',true,NULL,false),
    ('BUSINESS','ai.external.enabled',true,NULL,false),
    ('BUSINESS','ai.monthly_limit',true,1000,false),
    ('ENTERPRISE','team.members_limit',true,10,false),
    ('ENTERPRISE','clients.active_limit',true,NULL,true),
    ('ENTERPRISE','services.active_limit',true,NULL,true),
    ('ENTERPRISE','documents.monthly_limit',true,NULL,true),
    ('ENTERPRISE','pdf.monthly_limit',true,NULL,true),
    ('ENTERPRISE','pdf.watermark',false,NULL,false),
    ('ENTERPRISE','history.days_visible',true,NULL,true),
    ('ENTERPRISE','templates.basic_limit',true,NULL,true),
    ('ENTERPRISE','templates.custom_enabled',true,NULL,false),
    ('ENTERPRISE','branding.custom_logo',true,NULL,false),
    ('ENTERPRISE','numbering.custom_prefix',true,NULL,false),
    ('ENTERPRISE','public_approval.enabled',true,NULL,false),
    ('ENTERPRISE','public_approval.monthly_limit',true,NULL,true),
    ('ENTERPRISE','sharing.whatsapp',true,NULL,false),
    ('ENTERPRISE','sharing.public_link',true,NULL,false),
    ('ENTERPRISE','work_orders.enabled',true,NULL,false),
    ('ENTERPRISE','payments.manual.enabled',true,NULL,false),
    ('ENTERPRISE','receipts.operational.enabled',true,NULL,false),
    ('ENTERPRISE','document.convert_to_receipt',true,NULL,false),
    ('ENTERPRISE','reports.basic',true,NULL,false),
    ('ENTERPRISE','reports.advanced',true,NULL,false),
    ('ENTERPRISE','commercial.pipeline',true,NULL,false),
    ('ENTERPRISE','commercial.followups',true,NULL,false),
    ('ENTERPRISE','commercial.metrics',true,NULL,false),
    ('ENTERPRISE','exports.csv',true,NULL,false),
    ('ENTERPRISE','audit.account',true,NULL,false),
    ('ENTERPRISE','support.priority',true,NULL,false),
    ('ENTERPRISE','ai.external.enabled',true,NULL,false),
    ('ENTERPRISE','ai.monthly_limit',true,NULL,true)
) AS v(plan_code, feature_code, boolean_value, integer_value, is_unlimited)
JOIN orcafacil.plans p ON p.code = v.plan_code
JOIN orcafacil.plan_versions pv ON pv.plan_id = p.id AND pv.version_number = 1 AND pv.is_deleted = false
JOIN orcafacil.features f ON f.code = v.feature_code
ON CONFLICT (plan_version_id, feature_id) DO UPDATE SET
    boolean_value = EXCLUDED.boolean_value,
    integer_value = EXCLUDED.integer_value,
    is_unlimited  = EXCLUDED.is_unlimited,
    is_deleted    = false,
    updated_at    = now();

COMMIT;
