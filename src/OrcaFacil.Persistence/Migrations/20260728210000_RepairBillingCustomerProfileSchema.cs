using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

/// <summary>
/// Repairs installations where billing_customer_profiles already existed when the registration
/// stabilization migration ran.  This migration is deliberately additive and safe to re-run.
/// </summary>
[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260728210000_RepairBillingCustomerProfileSchema")]
public sealed class RepairBillingCustomerProfileSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS account_id uuid;
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS user_id uuid;
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS person_type varchar(30);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS document_type varchar(10);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS document_number varchar(20);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS name varchar(180);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS trade_name varchar(180);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS legal_name varchar(180);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS email varchar(254);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS phone varchar(40);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS city varchar(120);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS state varchar(2);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS postal_code varchar(8);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS street varchar(180);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS street_number varchar(30);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS complement varchar(120);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS district varchar(120);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS address varchar(300);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS mercado_pago_customer_id varchar(180);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS created_at timestamptz;
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS updated_at timestamptz;
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS is_deleted boolean;

            CREATE UNIQUE INDEX IF NOT EXISTS uq_billing_profiles_account_id
                ON orcafacil.billing_customer_profiles(account_id)
                WHERE account_id IS NOT NULL AND is_deleted = false;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // A rollback must never destroy billing data. Leaving nullable additive columns is safe.
    }
}
