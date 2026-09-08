-- OrçaFácil V6.6 - SaaS modular enterprise (aditivo, idempotente e sem DROP).
CREATE SCHEMA IF NOT EXISTS orcafacil;

ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS alternate_email varchar(254);
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS document_type varchar(16);
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS document_number varchar(14);
ALTER TABLE orcafacil.users DROP CONSTRAINT IF EXISTS ck_users_role;
ALTER TABLE orcafacil.users ADD CONSTRAINT ck_users_role CHECK (role IN ('User','Admin','SuperAdmin','GlobalSupport','GlobalBilling','GlobalAuditor'));
CREATE UNIQUE INDEX IF NOT EXISTS ux_users_alternate_email ON orcafacil.users(alternate_email) WHERE alternate_email IS NOT NULL AND is_deleted=false;
CREATE UNIQUE INDEX IF NOT EXISTS ux_users_document_number ON orcafacil.users(document_number) WHERE document_number IS NOT NULL AND is_deleted=false;
ALTER TABLE orcafacil.business_accounts ADD COLUMN IF NOT EXISTS city varchar(120);
ALTER TABLE orcafacil.business_accounts ADD COLUMN IF NOT EXISTS state varchar(2);
ALTER TABLE orcafacil.business_accounts ADD COLUMN IF NOT EXISTS responsible_name varchar(160);
ALTER TABLE orcafacil.business_accounts ADD COLUMN IF NOT EXISTS internal_notes varchar(2000);
ALTER TABLE orcafacil.business_accounts ADD COLUMN IF NOT EXISTS financial_status varchar(32) NOT NULL DEFAULT 'Current';
ALTER TABLE orcafacil.business_accounts ADD COLUMN IF NOT EXISTS last_access_at timestamptz;

CREATE TABLE IF NOT EXISTS orcafacil.saas_modules (
 id uuid PRIMARY KEY, code varchar(80) NOT NULL, display_name varchar(160) NOT NULL, description varchar(1000) NOT NULL,
 category varchar(80) NOT NULL, icon_key varchar(80) NOT NULL, menu_group varchar(80) NOT NULL, route_prefix varchar(160) NOT NULL,
 required_permission_code varchar(160) NOT NULL, base_monthly_price numeric(18,2) NOT NULL, base_annual_price numeric(18,2) NOT NULL,
 is_active boolean NOT NULL DEFAULT true, is_public boolean NOT NULL DEFAULT true, display_order integer NOT NULL DEFAULT 0,
 deleted_at timestamptz, deleted_by uuid, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_saas_modules_code ON orcafacil.saas_modules(code);
CREATE INDEX IF NOT EXISTS ix_saas_modules_status_order ON orcafacil.saas_modules(is_active,display_order);

CREATE TABLE IF NOT EXISTS orcafacil.saas_module_features (
 id uuid PRIMARY KEY, module_id uuid NOT NULL REFERENCES orcafacil.saas_modules(id), code varchar(120) NOT NULL, display_name varchar(160) NOT NULL,
 required_permission_code varchar(160) NOT NULL, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_saas_module_features_module_code ON orcafacil.saas_module_features(module_id,code);

CREATE TABLE IF NOT EXISTS orcafacil.saas_module_prices (
 id uuid PRIMARY KEY, module_id uuid NOT NULL REFERENCES orcafacil.saas_modules(id), billing_period varchar(16) NOT NULL, amount numeric(18,2) NOT NULL,
 currency varchar(3) NOT NULL DEFAULT 'BRL', valid_from timestamptz NOT NULL, valid_until timestamptz, is_active boolean NOT NULL DEFAULT true,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_saas_module_prices_module_period ON orcafacil.saas_module_prices(module_id,billing_period,valid_from);

CREATE TABLE IF NOT EXISTS orcafacil.account_module_subscriptions (
 id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), module_id uuid NOT NULL REFERENCES orcafacil.saas_modules(id),
 status varchar(32) NOT NULL, billing_period varchar(16) NOT NULL, contracted_price numeric(18,2) NOT NULL, discount_amount numeric(18,2) NOT NULL DEFAULT 0,
 currency varchar(3) NOT NULL DEFAULT 'BRL', starts_at timestamptz, trial_ends_at timestamptz, manual_grant_ends_at timestamptz,
 manual_grant_reason varchar(500), suspended_at timestamptz, cancelled_at timestamptz, changed_by_user_id uuid,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_module_subscriptions_active ON orcafacil.account_module_subscriptions(account_id,module_id) WHERE is_deleted=false;
CREATE INDEX IF NOT EXISTS ix_account_module_subscriptions_status ON orcafacil.account_module_subscriptions(account_id,status);

CREATE TABLE IF NOT EXISTS orcafacil.account_module_entitlements (
 id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), module_id uuid NOT NULL REFERENCES orcafacil.saas_modules(id),
 feature_code varchar(120), is_enabled boolean NOT NULL DEFAULT true, source varchar(24) NOT NULL, valid_until timestamptz, reason varchar(500), granted_by_user_id uuid NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_module_entitlements ON orcafacil.account_module_entitlements(account_id,module_id,COALESCE(feature_code,'')) WHERE is_deleted=false;

CREATE TABLE IF NOT EXISTS orcafacil.account_module_feature_limits (
 id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), module_id uuid NOT NULL REFERENCES orcafacil.saas_modules(id),
 feature_code varchar(120) NOT NULL, limit_value bigint NOT NULL, period varchar(24) NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_module_feature_limits ON orcafacil.account_module_feature_limits(account_id,module_id,feature_code,period) WHERE is_deleted=false;

CREATE TABLE IF NOT EXISTS orcafacil.account_module_usage_events (
 id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), user_id uuid, module_code varchar(80) NOT NULL,
 feature_code varchar(120), event_code varchar(120) NOT NULL, route varchar(300), correlation_id varchar(100) NOT NULL, occurred_at timestamptz NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_account_module_usage_module ON orcafacil.account_module_usage_events(account_id,module_code,occurred_at);
CREATE INDEX IF NOT EXISTS ix_account_module_usage_user ON orcafacil.account_module_usage_events(account_id,user_id,occurred_at);

CREATE TABLE IF NOT EXISTS orcafacil.account_module_usage_snapshots (
 id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), module_code varchar(80) NOT NULL,
 period_start timestamptz NOT NULL, period_end timestamptz NOT NULL, event_count bigint NOT NULL, active_users integer NOT NULL, last_used_at timestamptz,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_module_usage_snapshots ON orcafacil.account_module_usage_snapshots(account_id,module_code,period_start) WHERE is_deleted=false;

CREATE TABLE IF NOT EXISTS orcafacil.account_module_audit_logs (
 id uuid PRIMARY KEY, account_id uuid, actor_user_id uuid NOT NULL, action varchar(120) NOT NULL, entity_type varchar(120) NOT NULL,
 entity_id varchar(100), module_code varchar(80), summary varchar(1000) NOT NULL, correlation_id varchar(100) NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_account_module_audit_account ON orcafacil.account_module_audit_logs(account_id,created_at);
CREATE INDEX IF NOT EXISTS ix_account_module_audit_action ON orcafacil.account_module_audit_logs(action,created_at);

CREATE TABLE IF NOT EXISTS orcafacil.account_member_profiles (
 id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), account_member_id uuid NOT NULL REFERENCES orcafacil.account_members(id),
 role_id uuid NOT NULL REFERENCES orcafacil.roles(id), assigned_by_user_id uuid NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_member_profiles ON orcafacil.account_member_profiles(account_id,account_member_id,role_id) WHERE is_deleted=false;

INSERT INTO orcafacil.roles(code,display_name,is_platform_role,is_system,created_at,is_deleted) VALUES
 ('Manager','Gestor',false,true,now(),false),('Commercial','Comercial',false,true,now(),false),
 ('Operations','Operações',false,true,now(),false),('Technician','Técnico',false,true,now(),false),
 ('Financial','Financeiro',false,true,now(),false),('Fiscal','Fiscal',false,true,now(),false),
 ('CustomerSuccess','Customer Success',false,true,now(),false),('Support','Suporte',false,true,now(),false),
 ('ReadOnly','Somente leitura',false,true,now(),false),('ClientPortalUser','Portal do cliente',false,true,now(),false),
 ('PartnerPortalUser','Portal do parceiro',false,true,now(),false)
ON CONFLICT(code) DO UPDATE SET display_name=EXCLUDED.display_name,is_deleted=false;

INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission,created_at,is_deleted) VALUES
 ('Global.Clients.View','Ver clientes globais',true,now(),false),('Global.Clients.Manage','Gerenciar clientes globais',true,now(),false),
 ('Global.Users.View','Ver usuários globais',true,now(),false),('Global.Users.Manage','Gerenciar usuários globais',true,now(),false),
 ('Global.Modules.View','Ver módulos globais',true,now(),false),('Global.Modules.Manage','Gerenciar módulos globais',true,now(),false),
 ('Global.Billing.View','Ver billing global',true,now(),false),('Global.Billing.Manage','Gerenciar billing global',true,now(),false),
 ('Global.Usage.View','Ver uso global',true,now(),false),('Global.Audit.View','Ver auditoria global',true,now(),false),
 ('Account.Users.View','Ver usuários da conta',false,now(),false),('Account.Users.Manage','Gerenciar usuários da conta',false,now(),false),
 ('Account.Profiles.Manage','Gerenciar perfis da conta',false,now(),false),('Account.Modules.View','Ver módulos da conta',false,now(),false),('Account.Billing.View','Ver billing da conta',false,now(),false),
 ('Clients.Create','Criar clientes',false,now(),false),('Clients.Edit','Editar clientes',false,now(),false),('Clients.Delete','Excluir clientes',false,now(),false),
 ('Documents.Delete','Excluir documentos',false,now(),false),('Documents.Approve','Aprovar documentos',false,now(),false),('Documents.Send','Enviar documentos',false,now(),false),('Documents.Convert','Converter documentos',false,now(),false),
 ('CommercialRoutine.View','Ver rotina comercial',false,now(),false),('CommercialRoutine.Manage','Gerenciar rotina comercial',false,now(),false),
 ('Projects.View','Ver projetos',false,now(),false),('Projects.Manage','Gerenciar projetos',false,now(),false),('Portal.View','Acessar portal',false,now(),false),
 ('Training.View','Ver treinamento',false,now(),false),('QualityGate.View','Ver quality gate',false,now(),false),('SystemHealth.View','Ver saúde do sistema',false,now(),false)
ON CONFLICT(code) DO UPDATE SET display_name=EXCLUDED.display_name,is_platform_permission=EXCLUDED.is_platform_permission,is_deleted=false;

INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted)
SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p
WHERE r.code IN ('Owner','Administrator') AND p.is_platform_permission=false
ON CONFLICT(role_id,permission_id) DO NOTHING;
INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted)
SELECT r.id,p.id,now(),false FROM orcafacil.roles r JOIN orcafacil.permissions p ON
 (r.code='Manager' AND p.code NOT LIKE 'Global.%') OR
 (r.code='Commercial' AND (p.code LIKE 'Clients.%' OR p.code LIKE 'Documents.%' OR p.code LIKE 'CommercialRoutine.%' OR p.code='Dashboard.View')) OR
 (r.code IN ('Operations','Technician') AND (p.code LIKE 'WorkOrders.%' OR p.code LIKE 'Schedule.%' OR p.code='Dashboard.View')) OR
 (r.code='Financial' AND (p.code LIKE 'Finance.%' OR p.code LIKE 'Payments.%' OR p.code LIKE 'Receipts.%' OR p.code='Dashboard.View')) OR
 (r.code='Fiscal' AND (p.code LIKE 'Fiscal.%' OR p.code='Dashboard.View')) OR
 (r.code='CustomerSuccess' AND (p.code LIKE 'CustomerSuccess.%' OR p.code='Dashboard.View')) OR
 (r.code='Support' AND (p.code LIKE 'Support.%' OR p.code='Dashboard.View')) OR
 (r.code='ReadOnly' AND p.code IN ('Dashboard.View','Clients.View','Documents.View','WorkOrders.View','Finance.View','Fiscal.View')) OR
 (r.code='ClientPortalUser' AND p.code='Portal.View') OR (r.code='PartnerPortalUser' AND p.code='Partners.View')
ON CONFLICT(role_id,permission_id) DO NOTHING;

WITH module_seed(code,display_name,category,icon_key,menu_group,route_prefix,permission,monthly,annual,ordering,is_public,is_active) AS (VALUES
 ('CORE','Core','Base','home','Principal','/Dashboard','Dashboard.View',0,0,10,false,true),
 ('CLIENTS','Clientes','Comercial','users','Comercial','/Clients','Clients.View',39,390,20,true,true),
 ('DOCUMENTS','Orçamentos e Propostas','Comercial','file-text','Comercial','/Documents','Documents.View',69,690,30,true,true),
 ('COMMERCIAL_ROUTINE','Rotina Comercial','Comercial','trending-up','Comercial','/CommercialRoutine','CommercialRoutine.View',49,490,40,true,true),
 ('WORK_ORDERS','Ordens de Serviço','Operações','tool','Operações','/WorkOrders','WorkOrders.View',79,790,50,true,true),
 ('SCHEDULE','Agenda e Campo','Operações','calendar','Operações','/Schedule','Schedule.View',49,490,60,true,true),
 ('FINANCIAL','Financeiro','Gestão','dollar-sign','Gestão','/CashFlow','Finance.View',89,890,70,true,true),
 ('FISCAL','Fiscal','Gestão','clipboard','Gestão','/Fiscal','Fiscal.View',99,990,80,true,false),
 ('PROJECTS','Projetos','Gestão','briefcase','Gestão','/Projects','Projects.View',69,690,90,true,false),
 ('CUSTOMER_SUCCESS','Customer Success','Relacionamento','heart','Relacionamento','/CustomerSuccess','CustomerSuccess.View',59,590,100,true,false),
 ('BI','BI Executivo','Inteligência','bar-chart','Inteligência','/Bi','BI.View',99,990,110,true,false),
 ('AUTOMATION','Automação','Inteligência','zap','Inteligência','/Automation','Automation.View',79,790,120,true,false),
 ('DATA_GOVERNANCE','Governança de Dados','Governança','shield','Governança','/DataGovernance','DataQuality.View',89,890,130,true,false),
 ('CLIENT_PORTAL','Portal do Cliente','Portais','external-link','Portais','/Portal','Portal.View',49,490,140,true,false),
 ('PARTNER_PORTAL','Portal do Parceiro','Portais','link','Portais','/Partners','Partners.View',49,490,150,true,false),
 ('SUPPORT','Suporte','Sucesso','help-circle','Sucesso','/Support','Support.View',29,290,160,true,true),
 ('TRAINING','Treinamento','Sucesso','book-open','Sucesso','/Training','Training.View',19,190,170,true,true),
 ('ACCOUNT_ADMIN','Administração','Administração','settings','Administração','/AccountAdmin','Account.Users.View',0,0,180,false,true),
 ('QUALITY_GATE','Quality Gate','Governança','check-circle','Governança','/Admin/QualityGate','QualityGate.View',0,0,190,false,true))
INSERT INTO orcafacil.saas_modules(id,code,display_name,description,category,icon_key,menu_group,route_prefix,required_permission_code,base_monthly_price,base_annual_price,is_active,is_public,display_order,created_at,is_deleted)
SELECT md5('saas-module:'||code)::uuid,code,display_name,display_name,category,icon_key,menu_group,route_prefix,permission,monthly,annual,is_active,is_public,ordering,now(),false FROM module_seed
ON CONFLICT (code) DO UPDATE SET display_name=EXCLUDED.display_name, route_prefix=EXCLUDED.route_prefix, required_permission_code=EXCLUDED.required_permission_code, is_active=EXCLUDED.is_active, updated_at=now();

INSERT INTO orcafacil.saas_module_prices(id,module_id,billing_period,amount,currency,valid_from,is_active,created_at,is_deleted)
SELECT md5('saas-price-monthly:'||code)::uuid,id,'Monthly',base_monthly_price,'BRL',now(),true,now(),false FROM orcafacil.saas_modules
ON CONFLICT (id) DO NOTHING;
INSERT INTO orcafacil.saas_module_prices(id,module_id,billing_period,amount,currency,valid_from,is_active,created_at,is_deleted)
SELECT md5('saas-price-annual:'||code)::uuid,id,'Annual',base_annual_price,'BRL',now(),true,now(),false FROM orcafacil.saas_modules
ON CONFLICT (id) DO NOTHING;

-- Preserve existing customers: their previous all-in-one access becomes an explicit grandfathered contract.
INSERT INTO orcafacil.account_module_subscriptions(id,account_id,module_id,status,billing_period,contracted_price,discount_amount,currency,starts_at,created_at,is_deleted)
SELECT md5(a.id::text||':'||m.id::text)::uuid,a.id,m.id,'Active','Monthly',0,0,'BRL',now(),now(),false
FROM orcafacil.business_accounts a CROSS JOIN orcafacil.saas_modules m WHERE a.is_deleted=false
ON CONFLICT DO NOTHING;
INSERT INTO orcafacil.account_module_entitlements(id,account_id,module_id,feature_code,is_enabled,source,reason,granted_by_user_id,created_at,is_deleted)
SELECT md5('entitlement:'||a.id::text||':'||m.id::text)::uuid,a.id,m.id,NULL,true,'Subscription','Migração V6.6: acesso preexistente',COALESCE((SELECT am.user_id FROM orcafacil.account_members am WHERE am.account_id=a.id AND am.is_deleted=false ORDER BY am.created_at LIMIT 1),'00000000-0000-0000-0000-000000000000'::uuid),now(),false
FROM orcafacil.business_accounts a CROSS JOIN orcafacil.saas_modules m WHERE a.is_deleted=false
ON CONFLICT DO NOTHING;
