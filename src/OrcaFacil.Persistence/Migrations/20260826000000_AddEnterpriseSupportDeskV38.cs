using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

/// <summary>Non-destructive Sprint 37 bridge. The complete idempotent production patch lives in database/sprint37_support_desk_v38.sql.</summary>
public partial class AddEnterpriseSupportDeskV38 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name:"QueueId",schema:"orcafacil",table:"support_tickets",type:"uuid",nullable:true);
        migrationBuilder.AddColumn<Guid>(name:"AssignedToUserId",schema:"orcafacil",table:"support_tickets",type:"uuid",nullable:true);
        migrationBuilder.AddColumn<Guid>(name:"SlaPolicyId",schema:"orcafacil",table:"support_tickets",type:"uuid",nullable:true);
        migrationBuilder.AddColumn<DateTime>(name:"FirstResponseDueAt",schema:"orcafacil",table:"support_tickets",type:"timestamp with time zone",nullable:true);
        migrationBuilder.AddColumn<DateTime>(name:"ResolutionDueAt",schema:"orcafacil",table:"support_tickets",type:"timestamp with time zone",nullable:true);
        migrationBuilder.AddColumn<DateTime>(name:"FirstRespondedAt",schema:"orcafacil",table:"support_tickets",type:"timestamp with time zone",nullable:true);
        migrationBuilder.AddColumn<string>(name:"Source",schema:"orcafacil",table:"support_tickets",type:"character varying(32)",maxLength:32,nullable:false,defaultValue:"Internal");
        migrationBuilder.AddColumn<string>(name:"Impact",schema:"orcafacil",table:"support_tickets",type:"character varying(24)",maxLength:24,nullable:false,defaultValue:"SingleUser");
        migrationBuilder.AddColumn<string>(name:"Urgency",schema:"orcafacil",table:"support_tickets",type:"character varying(24)",maxLength:24,nullable:false,defaultValue:"Normal");
        migrationBuilder.AddColumn<string>(name:"Type",schema:"orcafacil",table:"support_ticket_messages",type:"character varying(24)",maxLength:24,nullable:false,defaultValue:"PublicReply");
        migrationBuilder.Sql("CREATE TABLE IF NOT EXISTS orcafacil.support_queues (\"Id\" uuid PRIMARY KEY, \"AccountId\" uuid NULL, \"Name\" varchar(120) NOT NULL, \"Description\" text NOT NULL DEFAULT '', \"Level\" varchar(16) NOT NULL, \"IsGlobal\" boolean NOT NULL DEFAULT false, \"IsActive\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamptz NOT NULL DEFAULT now(), \"UpdatedAt\" timestamptz NULL, \"IsDeleted\" boolean NOT NULL DEFAULT false);");
        migrationBuilder.Sql("CREATE TABLE IF NOT EXISTS orcafacil.support_ticket_sla_events (\"Id\" uuid PRIMARY KEY, \"AccountId\" uuid NOT NULL, \"TicketId\" uuid NOT NULL, \"Type\" varchar(50) NOT NULL, \"OccurredAt\" timestamptz NOT NULL, \"CreatedAt\" timestamptz NOT NULL DEFAULT now(), \"UpdatedAt\" timestamptz NULL, \"IsDeleted\" boolean NOT NULL DEFAULT false);");
        migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ux_support_sla_event_once ON orcafacil.support_ticket_sla_events(\"TicketId\",\"Type\") WHERE NOT \"IsDeleted\";");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("A evolução V3.8 é preservadora; use restauração testada em vez de excluir dados de suporte.");
}
