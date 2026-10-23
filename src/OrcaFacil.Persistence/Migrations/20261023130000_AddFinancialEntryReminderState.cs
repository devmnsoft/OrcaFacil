using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

/// <summary>Tracks dunning cadence on the receivable so reminders are bounded and never repeat too soon.</summary>
public partial class AddFinancialEntryReminderState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(name: "last_reminder_at", schema: "orcafacil", table: "financial_entries", type: "date", nullable: true);
        migrationBuilder.AddColumn<int>(name: "reminder_count", schema: "orcafacil", table: "financial_entries", type: "integer", nullable: false, defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "last_reminder_at", schema: "orcafacil", table: "financial_entries");
        migrationBuilder.DropColumn(name: "reminder_count", schema: "orcafacil", table: "financial_entries");
    }
}
