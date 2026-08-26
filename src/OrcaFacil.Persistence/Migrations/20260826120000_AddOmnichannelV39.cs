using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

/// <summary>Non-destructive V3.9 bridge; the complete idempotent patch is database/sprint38_omnichannel_v39.sql.</summary>
public partial class AddOmnichannelV39 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TABLE IF NOT EXISTS orcafacil.omnichannel_channels (id uuid PRIMARY KEY, account_id uuid NULL, type varchar(32) NOT NULL, status varchar(24) NOT NULL DEFAULT 'NotConfigured', name varchar(120) NOT NULL, is_global boolean NOT NULL DEFAULT false, is_enabled boolean NOT NULL DEFAULT false, last_health_check_at timestamptz NULL, last_failure text NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);");
        migrationBuilder.Sql("CREATE TABLE IF NOT EXISTS orcafacil.omnichannel_conversations (id uuid PRIMARY KEY, account_id uuid NOT NULL, channel_id uuid NOT NULL, status varchar(24) NOT NULL DEFAULT 'New', subject varchar(200) NOT NULL DEFAULT '', priority varchar(24) NOT NULL DEFAULT 'Normal', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_omnichannel_conversations_account ON orcafacil.omnichannel_conversations(account_id,status);");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("V3.9 preserva mensagens e conversas; restaure um backup testado para rollback.");
}
