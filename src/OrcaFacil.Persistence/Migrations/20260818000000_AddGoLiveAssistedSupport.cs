using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace OrcaFacil.Persistence.Migrations;
public partial class AddGoLiveAssistedSupport : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
ALTER TABLE orcafacil.support_tickets ADD COLUMN IF NOT EXISTS related_page varchar(300);
ALTER TABLE orcafacil.support_tickets ADD COLUMN IF NOT EXISTS correlation_id varchar(100);
ALTER TABLE orcafacil.support_tickets ADD COLUMN IF NOT EXISTS browser_info varchar(500);
CREATE TABLE IF NOT EXISTS orcafacil.user_feedback (id uuid PRIMARY KEY, account_id uuid NULL, user_id uuid NULL, page_url varchar(500) NOT NULL, rating varchar(32) NOT NULL, message varchar(2000), browser_info varchar(500), correlation_id varchar(100), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_user_feedback_account_created ON orcafacil.user_feedback(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.knowledge_base_articles (id uuid PRIMARY KEY, title varchar(180) NOT NULL, slug varchar(180) NOT NULL, summary varchar(500) NOT NULL, content varchar(12000) NOT NULL, category varchar(80) NOT NULL, audience varchar(24) NOT NULL DEFAULT 'All', is_published boolean NOT NULL DEFAULT false, display_order integer NOT NULL DEFAULT 0, published_at timestamptz NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_knowledge_base_articles_slug ON orcafacil.knowledge_base_articles(slug);
CREATE TABLE IF NOT EXISTS orcafacil.release_notes (id uuid PRIMARY KEY, version varchar(30) NOT NULL, title varchar(180) NOT NULL, description varchar(5000) NOT NULL, released_at timestamptz NOT NULL, category varchar(32) NOT NULL, is_published boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_release_notes_published_release ON orcafacil.release_notes(is_published,released_at DESC);
""");
 protected override void Down(MigrationBuilder migrationBuilder) { }
}
