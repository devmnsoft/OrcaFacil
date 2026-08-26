-- Sprint 35 / V3.6. Additive, idempotent and non-destructive localization schema.
CREATE SCHEMA IF NOT EXISTS orcafacil;
CREATE TABLE IF NOT EXISTS orcafacil.localization_languages (
 id uuid PRIMARY KEY, code varchar(16) NOT NULL, name varchar(100) NOT NULL, native_name varchar(100) NOT NULL,
 is_default boolean NOT NULL DEFAULT false, is_active boolean NOT NULL DEFAULT true,
 is_public_enabled boolean NOT NULL DEFAULT false, is_portal_enabled boolean NOT NULL DEFAULT false,
 is_admin_enabled boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_localization_languages_code ON orcafacil.localization_languages(lower(code)) WHERE NOT is_deleted;
CREATE UNIQUE INDEX IF NOT EXISTS ux_localization_languages_default ON orcafacil.localization_languages(is_default) WHERE is_default AND NOT is_deleted;

INSERT INTO orcafacil.localization_languages(id,code,name,native_name,is_default,is_active,is_public_enabled,is_portal_enabled,is_admin_enabled)
VALUES
 ('35000000-0000-0000-0000-000000000001','pt-BR','Portuguese (Brazil)','Português (Brasil)',true,true,true,true,true),
 ('35000000-0000-0000-0000-000000000002','en-US','English (United States)','English (United States)',false,true,true,true,true),
 ('35000000-0000-0000-0000-000000000003','es-ES','Spanish (Spain)','Español (España)',false,true,true,true,true),
 ('35000000-0000-0000-0000-000000000004','es-419','Spanish (Latin America)','Español (Latinoamérica)',false,true,true,true,true)
ON CONFLICT DO NOTHING;

CREATE TABLE IF NOT EXISTS orcafacil.localization_resources (
 id uuid PRIMARY KEY, resource_key varchar(240) NOT NULL, module varchar(80) NOT NULL, is_html boolean NOT NULL DEFAULT false,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE UNIQUE INDEX IF NOT EXISTS ux_localization_resource_key ON orcafacil.localization_resources(resource_key);
CREATE TABLE IF NOT EXISTS orcafacil.localization_resource_values (
 id uuid PRIMARY KEY, resource_id uuid NOT NULL REFERENCES orcafacil.localization_resources(id), language_code varchar(16) NOT NULL,
 value text NOT NULL, status varchar(24) NOT NULL DEFAULT 'Draft', version integer NOT NULL DEFAULT 1,
 published_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE UNIQUE INDEX IF NOT EXISTS ux_localization_value ON orcafacil.localization_resource_values(resource_id,language_code,version);
CREATE TABLE IF NOT EXISTS orcafacil.localization_missing_keys (
 id uuid PRIMARY KEY, resource_key varchar(240) NOT NULL, language_code varchar(16) NOT NULL, source varchar(300),
 occurrences integer NOT NULL DEFAULT 1, first_seen_at timestamptz NOT NULL DEFAULT now(), last_seen_at timestamptz NOT NULL DEFAULT now(), resolved_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_localization_missing_open ON orcafacil.localization_missing_keys(resource_key,language_code) WHERE resolved_at IS NULL;
CREATE TABLE IF NOT EXISTS orcafacil.localization_audit_events (
 id uuid PRIMARY KEY, account_id uuid, actor_user_id uuid, action varchar(80) NOT NULL, entity_type varchar(80) NOT NULL,
 entity_id uuid, language_code varchar(16), details_json jsonb NOT NULL DEFAULT '{}', created_at timestamptz NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS orcafacil.account_locale_settings (
 account_id uuid PRIMARY KEY, language_code varchar(16) NOT NULL DEFAULT 'pt-BR', culture_code varchar(16) NOT NULL DEFAULT 'pt-BR',
 currency_code char(3) NOT NULL DEFAULT 'BRL', time_zone_id varchar(100) NOT NULL DEFAULT 'America/Sao_Paulo',
 date_format varchar(32) NOT NULL DEFAULT 'd', time_format varchar(32) NOT NULL DEFAULT 't', updated_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS orcafacil.user_locale_preferences (
 user_id uuid PRIMARY KEY, account_id uuid NOT NULL, language_code varchar(16) NOT NULL,
 culture_code varchar(16), currency_code char(3), time_zone_id varchar(100), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_user_locale_account ON orcafacil.user_locale_preferences(account_id,user_id);
CREATE TABLE IF NOT EXISTS orcafacil.portal_locale_preferences (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, subject_type varchar(16) NOT NULL CHECK(subject_type IN ('Client','Partner')),
 subject_id uuid NOT NULL, language_code varchar(16) NOT NULL, culture_code varchar(16), time_zone_id varchar(100), updated_at timestamptz NOT NULL DEFAULT now());
CREATE UNIQUE INDEX IF NOT EXISTS ux_portal_locale_subject ON orcafacil.portal_locale_preferences(account_id,subject_type,subject_id);

CREATE TABLE IF NOT EXISTS orcafacil.public_content_translations (
 id uuid PRIMARY KEY, account_id uuid, content_type varchar(40) NOT NULL, content_id uuid NOT NULL, language_code varchar(16) NOT NULL,
 title varchar(300) NOT NULL, slug varchar(300) NOT NULL, body text NOT NULL, status varchar(24) NOT NULL DEFAULT 'Draft',
 version integer NOT NULL DEFAULT 1, published_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE UNIQUE INDEX IF NOT EXISTS ux_public_translation_version ON orcafacil.public_content_translations(content_type,content_id,language_code,version);
CREATE UNIQUE INDEX IF NOT EXISTS ux_public_translation_slug ON orcafacil.public_content_translations(language_code,slug) WHERE status='Published';
CREATE TABLE IF NOT EXISTS orcafacil.template_translations (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, template_type varchar(32) NOT NULL, template_id uuid NOT NULL,
 language_code varchar(16) NOT NULL, subject varchar(300), body text NOT NULL, status varchar(24) NOT NULL DEFAULT 'Draft',
 version integer NOT NULL DEFAULT 1, created_at timestamptz NOT NULL DEFAULT now(), published_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_template_translation ON orcafacil.template_translations(account_id,template_type,template_id,language_code,version);
CREATE TABLE IF NOT EXISTS orcafacil.legal_content_translations (
 id uuid PRIMARY KEY, legal_content_id uuid NOT NULL, language_code varchar(16) NOT NULL, version integer NOT NULL,
 title varchar(300) NOT NULL, body text NOT NULL, status varchar(24) NOT NULL DEFAULT 'Draft', published_at timestamptz, created_at timestamptz NOT NULL DEFAULT now());
CREATE UNIQUE INDEX IF NOT EXISTS ux_legal_translation_version ON orcafacil.legal_content_translations(legal_content_id,language_code,version);
CREATE TABLE IF NOT EXISTS orcafacil.seo_metadata_translations (
 id uuid PRIMARY KEY, content_type varchar(40) NOT NULL, content_id uuid NOT NULL, language_code varchar(16) NOT NULL,
 meta_title varchar(300) NOT NULL, meta_description varchar(500) NOT NULL, canonical_path varchar(500) NOT NULL,
 og_locale varchar(24) NOT NULL, is_published boolean NOT NULL DEFAULT false, updated_at timestamptz NOT NULL DEFAULT now());
CREATE UNIQUE INDEX IF NOT EXISTS ux_seo_translation ON orcafacil.seo_metadata_translations(content_type,content_id,language_code);
CREATE TABLE IF NOT EXISTS orcafacil.translation_review_tasks (
 id uuid PRIMARY KEY, account_id uuid, resource_type varchar(40) NOT NULL, resource_id uuid NOT NULL,
 language_code varchar(16) NOT NULL, status varchar(24) NOT NULL DEFAULT 'Pending', assigned_user_id uuid,
 due_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_translation_review_open ON orcafacil.translation_review_tasks(resource_type,resource_id,language_code) WHERE status='Pending';

-- Dedicated translation stores retain explicit ownership and lifecycle per channel.
CREATE TABLE IF NOT EXISTS orcafacil.email_template_translations (LIKE orcafacil.template_translations INCLUDING DEFAULTS INCLUDING CONSTRAINTS);
CREATE TABLE IF NOT EXISTS orcafacil.message_template_translations (LIKE orcafacil.template_translations INCLUDING DEFAULTS INCLUDING CONSTRAINTS);
CREATE TABLE IF NOT EXISTS orcafacil.document_template_translations (LIKE orcafacil.template_translations INCLUDING DEFAULTS INCLUDING CONSTRAINTS);
CREATE UNIQUE INDEX IF NOT EXISTS ux_email_template_translation ON orcafacil.email_template_translations(account_id,template_id,language_code,version);
CREATE UNIQUE INDEX IF NOT EXISTS ux_message_template_translation ON orcafacil.message_template_translations(account_id,template_id,language_code,version);
CREATE UNIQUE INDEX IF NOT EXISTS ux_document_template_translation ON orcafacil.document_template_translations(account_id,template_id,language_code,version);
CREATE TABLE IF NOT EXISTS orcafacil.translation_jobs (
 id uuid PRIMARY KEY, account_id uuid, job_type varchar(40) NOT NULL, status varchar(24) NOT NULL DEFAULT 'Pending',
 idempotency_key varchar(160) NOT NULL, requested_by_user_id uuid, created_at timestamptz NOT NULL DEFAULT now(), started_at timestamptz, completed_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_translation_job_idempotency ON orcafacil.translation_jobs(idempotency_key);
CREATE TABLE IF NOT EXISTS orcafacil.translation_job_items (
 id uuid PRIMARY KEY, translation_job_id uuid NOT NULL REFERENCES orcafacil.translation_jobs(id), resource_type varchar(40) NOT NULL,
 resource_id uuid NOT NULL, language_code varchar(16) NOT NULL, status varchar(24) NOT NULL DEFAULT 'Pending', error_summary text);
CREATE UNIQUE INDEX IF NOT EXISTS ux_translation_job_item ON orcafacil.translation_job_items(translation_job_id,resource_type,resource_id,language_code);
