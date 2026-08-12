using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

public partial class AddCrmInteractionsAndSupport : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<Guid>(name:"converted_client_id",schema:"orcafacil",table:"commercial_leads",type:"uuid",nullable:true);
        m.AddForeignKey(name:"fk_commercial_leads_clients_converted_client_id",schema:"orcafacil",table:"commercial_leads",column:"converted_client_id",principalSchema:"orcafacil",principalTable:"clients",principalColumn:"id",onDelete:ReferentialAction.Restrict);
        m.CreateIndex(name:"ix_commercial_leads_converted_client_id",schema:"orcafacil",table:"commercial_leads",column:"converted_client_id");
        m.Sql("""
CREATE TABLE orcafacil.commercial_interactions (id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id) ON DELETE RESTRICT, lead_id uuid REFERENCES orcafacil.commercial_leads(id) ON DELETE RESTRICT, client_id uuid REFERENCES orcafacil.clients(id) ON DELETE RESTRICT, document_id uuid REFERENCES orcafacil.documents(id) ON DELETE RESTRICT, type varchar(24) NOT NULL, channel varchar(24) NOT NULL, summary varchar(1200) NOT NULL, next_follow_up_at timestamptz, created_by_user_id uuid NOT NULL REFERENCES orcafacil.users(id) ON DELETE RESTRICT, created_at timestamptz NOT NULL, updated_at timestamptz, completed_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX ix_commercial_interactions_account_id_next_follow_up_at ON orcafacil.commercial_interactions(account_id,next_follow_up_at);
CREATE TABLE orcafacil.support_tickets (id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id) ON DELETE RESTRICT, opened_by_user_id uuid NOT NULL REFERENCES orcafacil.users(id) ON DELETE RESTRICT, protocol varchar(24) NOT NULL UNIQUE, category varchar(24) NOT NULL, status varchar(24) NOT NULL, priority varchar(16) NOT NULL, subject varchar(180) NOT NULL, description varchar(5000) NOT NULL, internal_notes varchar(4000), resolved_at timestamptz, closed_at timestamptz, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX ix_support_tickets_account_id_status_created_at ON orcafacil.support_tickets(account_id,status,created_at);
CREATE TABLE orcafacil.support_ticket_messages (id uuid PRIMARY KEY, ticket_id uuid NOT NULL REFERENCES orcafacil.support_tickets(id) ON DELETE CASCADE, author_user_id uuid NOT NULL REFERENCES orcafacil.users(id) ON DELETE RESTRICT, body varchar(5000) NOT NULL, is_admin_reply boolean NOT NULL, is_internal boolean NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX ix_support_ticket_messages_ticket_id_created_at ON orcafacil.support_ticket_messages(ticket_id,created_at);
""");
    }
    protected override void Down(MigrationBuilder m) { m.DropTable(name:"support_ticket_messages",schema:"orcafacil"); m.DropTable(name:"support_tickets",schema:"orcafacil"); m.DropTable(name:"commercial_interactions",schema:"orcafacil"); m.DropForeignKey(name:"fk_commercial_leads_clients_converted_client_id",schema:"orcafacil",table:"commercial_leads"); m.DropIndex(name:"ix_commercial_leads_converted_client_id",schema:"orcafacil",table:"commercial_leads"); m.DropColumn(name:"converted_client_id",schema:"orcafacil",table:"commercial_leads"); }
}
