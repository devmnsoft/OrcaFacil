using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

public partial class AddDocumentFollowUps : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "follow_up_note", schema: "orcafacil", table: "documents", type: "character varying(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "follow_up_status", schema: "orcafacil", table: "documents", type: "character varying(24)", maxLength: 24, nullable: false, defaultValue: "None");
        migrationBuilder.AddColumn<DateTime>(name: "last_follow_up_at", schema: "orcafacil", table: "documents", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "next_follow_up_at", schema: "orcafacil", table: "documents", type: "timestamp with time zone", nullable: true);
        migrationBuilder.CreateIndex(name: "ix_documents_account_id_next_follow_up_at", schema: "orcafacil", table: "documents", columns: new[] { "account_id", "next_follow_up_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "ix_documents_account_id_next_follow_up_at", schema: "orcafacil", table: "documents");
        migrationBuilder.DropColumn(name: "follow_up_note", schema: "orcafacil", table: "documents");
        migrationBuilder.DropColumn(name: "follow_up_status", schema: "orcafacil", table: "documents");
        migrationBuilder.DropColumn(name: "last_follow_up_at", schema: "orcafacil", table: "documents");
        migrationBuilder.DropColumn(name: "next_follow_up_at", schema: "orcafacil", table: "documents");
    }
}
