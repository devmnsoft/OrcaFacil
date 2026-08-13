using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

public partial class AddDataImports : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE IF NOT EXISTS orcafacil.data_imports (
            id uuid PRIMARY KEY, account_id uuid NOT NULL, type varchar(24) NOT NULL,
            file_name varchar(255) NOT NULL, uploaded_by_user_id uuid NOT NULL,
            status varchar(32) NOT NULL, total_rows integer NOT NULL DEFAULT 0,
            imported_rows integer NOT NULL DEFAULT 0, skipped_rows integer NOT NULL DEFAULT 0,
            failed_rows integer NOT NULL DEFAULT 0, completed_at timestamptz,
            summary varchar(2000), staged_rows_json jsonb, errors_json jsonb,
            created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz,
            is_deleted boolean NOT NULL DEFAULT false
        );
        CREATE INDEX IF NOT EXISTS ix_data_imports_account_id_created_at
            ON orcafacil.data_imports(account_id, created_at);
        """);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("DROP TABLE IF EXISTS orcafacil.data_imports;");
}
