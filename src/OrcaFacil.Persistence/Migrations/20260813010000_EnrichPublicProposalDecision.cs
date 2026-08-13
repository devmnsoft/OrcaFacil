using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

public partial class EnrichPublicProposalDecision : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "accepted_terms", schema: "orcafacil", table: "public_document_decisions", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>(name: "customer_contact", schema: "orcafacil", table: "public_document_decisions", type: "character varying(254)", maxLength: 254, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "desired_date", schema: "orcafacil", table: "public_document_decisions", type: "date", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "accepted_terms", schema: "orcafacil", table: "public_document_decisions");
        migrationBuilder.DropColumn(name: "customer_contact", schema: "orcafacil", table: "public_document_decisions");
        migrationBuilder.DropColumn(name: "desired_date", schema: "orcafacil", table: "public_document_decisions");
    }
}
