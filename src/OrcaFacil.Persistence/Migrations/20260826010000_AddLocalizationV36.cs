using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

public partial class AddLocalizationV36 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE IF NOT EXISTS orcafacil.localization_languages (
          id uuid PRIMARY KEY, code varchar(16) NOT NULL UNIQUE, name varchar(100) NOT NULL, native_name varchar(100) NOT NULL,
          is_default boolean NOT NULL DEFAULT false, is_active boolean NOT NULL DEFAULT true,
          is_public_enabled boolean NOT NULL DEFAULT false, is_portal_enabled boolean NOT NULL DEFAULT false,
          is_admin_enabled boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(),
          updated_at timestamptz NOT NULL DEFAULT now(), is_deleted boolean NOT NULL DEFAULT false);
        CREATE UNIQUE INDEX IF NOT EXISTS ux_localization_languages_default ON orcafacil.localization_languages(is_default) WHERE is_default AND NOT is_deleted;
        CREATE TABLE IF NOT EXISTS orcafacil.account_locale_settings (
          account_id uuid PRIMARY KEY, language_code varchar(16) NOT NULL DEFAULT 'pt-BR', culture_code varchar(16) NOT NULL DEFAULT 'pt-BR',
          currency_code char(3) NOT NULL DEFAULT 'BRL', time_zone_id varchar(100) NOT NULL DEFAULT 'America/Sao_Paulo',
          date_format varchar(32) NOT NULL DEFAULT 'd', time_format varchar(32) NOT NULL DEFAULT 't', updated_at timestamptz NOT NULL DEFAULT now());
        CREATE TABLE IF NOT EXISTS orcafacil.user_locale_preferences (
          user_id uuid PRIMARY KEY, account_id uuid NOT NULL, language_code varchar(16) NOT NULL, culture_code varchar(16),
          currency_code char(3), time_zone_id varchar(100), updated_at timestamptz NOT NULL DEFAULT now());
        CREATE INDEX IF NOT EXISTS ix_user_locale_account ON orcafacil.user_locale_preferences(account_id,user_id);
        """);
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
