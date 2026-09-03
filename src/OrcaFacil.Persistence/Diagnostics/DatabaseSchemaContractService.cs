using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OrcaFacil.Application.Abstractions;

namespace OrcaFacil.Persistence.Diagnostics;

public sealed class DatabaseSchemaContractService(IConfiguration configuration) : IDatabaseSchemaContractService
{
    public const string RepairMigration = "20260728210000_RepairBillingCustomerProfileSchema";
    public const string PasswordRecoveryMigration = "20260728230000_AddPasswordRecoveryAndEmailOutbox";
    public const string CommercialJourneyMigration = "20260729000000_AddCommercialJourney";
    public const string QuoteToCashMigration = "20260730000000_AddManualPaymentsAndReceipts";
    public const string ReceiptSequenceMigration = "20260819010000_AddReceiptSequences";
    public const string CommercialDocumentRepairMigration = "20260902000000_AddMissingCommercialDocumentColumns";
    public const string ClientsAndDocumentsDriftMigration = "20260902010000_FixClientsAndDocumentsSchemaDrift";
    public const string DepositAmountDriftMigration = "20260902020000_FixDocumentsCommercialSchemaDepositAmount";
    public const string CommercialSchemaV57Migration = "20260903000000_FixCommercialSchemaDriftBudgetTemplatesAndDocumentsV57";
    public const string DocumentsRowVersionV60Migration = "20260903010000_FixDocumentsRowVersionSchemaDriftV60";
    public const string DocumentsFullSchemaV61Migration = "20260903020000_FixDocumentsTemplateCodeFullSchemaDriftV61";
    public const string QualityGateSchemaDriftV62Migration = "20260903030000_QualityGateSchemaDriftV62";

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> RegistrationContract =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["users"] = Columns(
                ("id", "uuid"), ("email", "character varying"), ("password_hash", "character varying"),
                ("failed_login_attempts", "integer"), ("last_failed_login_at", "timestamp with time zone"),
                ("last_successful_login_at", "timestamp with time zone"), ("locked_until", "timestamp with time zone"),
                ("is_blocked", "boolean"), ("block_reason", "character varying"), ("must_change_password", "boolean"),
                ("password_changed_at", "timestamp with time zone"), ("password_changed_by_user_id", "uuid"),
                ("password_expires_at", "timestamp with time zone"), ("password_reset_reason", "character varying"),
                ("session_version", "integer"), ("accepted_privacy_at", "timestamp with time zone"),
                ("accepted_terms_at", "timestamp with time zone"), ("legacy_unversioned_acceptance", "boolean")),
            ["business_accounts"] = Columns(("id", "uuid"), ("document_number", "character varying"), ("is_deleted", "boolean")),
            ["account_members"] = Columns(("id", "uuid"), ("account_id", "uuid"), ("user_id", "uuid"), ("role_code", "character varying")),
            ["documents"] = Columns(("id", "uuid"), ("account_id", "uuid"), ("user_id", "uuid"),
                ("number", "character varying"), ("type", "character varying"), ("status", "character varying"),
                ("subtotal", "numeric"), ("discount", "numeric"), ("total", "numeric"),
                ("issue_date", "timestamp with time zone"), ("valid_until", "timestamp with time zone"),
                ("client_id", "uuid"), ("client_name", "character varying"), ("client_document", "character varying"),
                ("client_email", "character varying"), ("client_phone", "character varying"), ("client_city", "character varying"),
                ("client_snapshot", "jsonb"), ("conditions_text", "text"), ("template_code", "character varying"),
                ("template_snapshot", "jsonb"), ("row_version", "bytea"), ("follow_up_status", "character varying"),
                ("follow_up_note", "character varying"), ("last_follow_up_at", "timestamp with time zone"),
                ("next_follow_up_at", "timestamp with time zone"), ("current_wizard_step", "integer"),
                ("last_autosave_key", "character varying"), ("last_autosaved_at", "timestamp with time zone"),
                ("public_enabled", "boolean"), ("public_token", "character varying"),
                ("client_decision", "character varying"), ("client_decision_at", "timestamp with time zone"),
                ("client_decision_note", "character varying"), ("internal_approval_status", "character varying"),
                ("requires_internal_approval", "boolean"), ("converted_receipt_id", "uuid"),
                ("converted_receipt_number", "character varying"), ("origin_budget_id", "uuid"),
                ("origin_budget_number", "character varying"), ("pix_information", "character varying"),
                ("evidence_hash", "character varying"), ("warranty_text", "character varying"),
                ("payment_method", "character varying"), ("deposit_amount", "numeric"), ("installment_count", "integer"),
                ("estimated_duration", "character varying"), ("expected_start_at", "timestamp with time zone"),
                ("assigned_team_id", "uuid"), ("assigned_to_user_id", "uuid"), ("business_unit_id", "uuid"),
                ("created_at", "timestamp with time zone"), ("updated_at", "timestamp with time zone"),
                ("deleted_at", "timestamp with time zone"), ("deleted_by", "uuid"), ("is_deleted", "boolean")),
            ["budget_templates"] = Columns(("id", "uuid"), ("account_id", "uuid"), ("user_id", "uuid"),
                ("title", "character varying"), ("profession", "character varying"), ("is_system_template", "boolean"), ("is_active", "boolean"), ("is_deleted", "boolean"),
                ("created_at", "timestamp with time zone"), ("updated_at", "timestamp with time zone"), ("deleted_at", "timestamp with time zone"), ("deleted_by", "uuid")),
            ["clients"] = Columns(("id", "uuid"), ("account_id", "uuid"), ("is_active", "boolean"), ("is_deleted", "boolean")),
            ["document_items"] = Columns(("id", "uuid"), ("document_id", "uuid")),
            ["budget_template_items"] = Columns(("id", "uuid"), ("template_id", "uuid")),
            ["contacts"] = Columns(("id", "uuid"), ("account_id", "uuid")),
            ["payments"] = Columns(("id", "uuid"), ("account_id", "uuid")),
            ["payment_invoices"] = Columns(("id", "uuid"), ("account_id", "uuid")),
            ["payment_receipts"] = Columns(("id", "uuid"), ("account_id", "uuid")),
            ["account_onboarding_states"] = Columns(("id", "uuid"), ("account_id", "uuid"), ("current_step", "character varying")),
            ["automation_rules"] = Columns(("id", "uuid"), ("account_id", "uuid")),
            ["customer_success_accounts"] = Columns(("id", "uuid"), ("account_id", "uuid")),
            ["service_catalog_items"] = Columns(("id", "uuid"), ("account_id", "uuid")),
            ["public_quotes"] = Columns(("id", "uuid"), ("document_id", "uuid")),
            ["billing_customer_profiles"] = Columns(
                ("id", "uuid"), ("account_id", "uuid"), ("user_id", "uuid"), ("person_type", "character varying"),
                ("document_type", "character varying"), ("document_number", "character varying"), ("name", "character varying"),
                ("trade_name", "character varying"), ("legal_name", "character varying"), ("email", "character varying"),
                ("phone", "character varying"), ("city", "character varying"), ("state", "character varying"),
                ("postal_code", "character varying"), ("street", "character varying"), ("street_number", "character varying"),
                ("complement", "character varying"), ("district", "character varying"), ("address", "character varying"),
                ("mercado_pago_customer_id", "character varying"), ("created_at", "timestamp with time zone"),
                ("updated_at", "timestamp with time zone"), ("is_deleted", "boolean")),
            ["subscriptions"] = Columns(("id", "uuid"), ("account_id", "uuid"), ("user_id", "uuid")),
            ["issuer_profiles"] = Columns(("id", "uuid"), ("user_id", "uuid")),
            ["notifications"] = Columns(("id", "uuid"), ("user_id", "uuid"), ("account_id", "uuid")),
            ["audit_logs"] = Columns(("id", "uuid"), ("user_id", "uuid"), ("account_id", "uuid"), ("correlation_id", "character varying")),
            ["features"] = Columns(("id", "uuid"), ("code", "character varying")),
            ["plans"] = Columns(("id", "uuid"), ("code", "character varying")),
            ["plan_versions"] = Columns(("id", "uuid"), ("plan_id", "uuid"), ("status", "character varying")),
            ["document_revisions"] = Columns(
                ("id", "uuid"), ("account_id", "uuid"), ("document_id", "uuid"), ("version_number", "integer"),
                ("status", "character varying"), ("snapshot_hash", "character varying"), ("protected_snapshot", "text"),
                ("branding_snapshot", "jsonb"), ("total", "numeric"), ("valid_until", "timestamp with time zone"),
                ("is_current", "boolean"), ("version", "xid")),
            ["public_document_accesses"] = Columns(
                ("id", "uuid"), ("account_id", "uuid"), ("document_id", "uuid"), ("document_revision_id", "uuid"),
                ("token_hash", "character varying"), ("expires_at", "timestamp with time zone"),
                ("revoked_at", "timestamp with time zone"), ("last_viewed_at", "timestamp with time zone"),
                ("view_count", "integer"), ("status", "character varying"), ("version", "xid")),
            ["public_document_decisions"] = Columns(
                ("id", "uuid"), ("account_id", "uuid"), ("document_id", "uuid"), ("document_revision_id", "uuid"),
                ("decision", "character varying"), ("customer_name", "character varying"), ("ip_hash", "character varying"),
                ("user_agent_hash", "character varying"), ("idempotency_key", "character varying")),
            ["commercial_follow_ups"] = Columns(
                ("id", "uuid"), ("account_id", "uuid"), ("document_id", "uuid"), ("document_revision_id", "uuid"),
                ("channel", "text"), ("result", "text"), ("occurred_at", "timestamp with time zone")),
            ["work_orders"] = Columns(
                ("id", "uuid"), ("account_id", "uuid"), ("source_document_id", "uuid"), ("source_revision_id", "uuid"),
                ("client_id", "uuid"), ("number", "character varying"), ("status", "character varying"),
                ("address_snapshot", "jsonb"), ("client_snapshot", "jsonb"), ("items_snapshot", "jsonb"),
                ("total_snapshot", "numeric"), ("payment_received", "boolean"), ("version", "xid")),
            ["manual_payments"] = Columns(
                ("id", "uuid"), ("account_id", "uuid"), ("work_order_id", "uuid"), ("document_id", "uuid"),
                ("client_id", "uuid"), ("amount", "numeric"), ("payment_method", "character varying"),
                ("paid_at", "timestamp with time zone"), ("registered_by_user_id", "uuid"), ("idempotency_key", "character varying")),
            ["receipts"] = Columns(
                ("id", "uuid"), ("account_id", "uuid"), ("payment_id", "uuid"), ("work_order_id", "uuid"),
                ("client_id", "uuid"), ("number", "character varying"), ("issuer_snapshot", "jsonb"),
                ("client_snapshot", "jsonb"), ("service_snapshot", "jsonb"), ("amount", "numeric")),
            ["receipt_sequences"] = Columns(
                ("id", "uuid"), ("account_id", "uuid"), ("year", "integer"),
                ("current_number", "bigint"), ("prefix", "character varying"),
                ("created_at", "timestamp with time zone"), ("is_deleted", "boolean")),
            ["password_reset_tokens"] = Columns(
                ("id", "uuid"), ("user_id", "uuid"), ("token_hash", "character varying"),
                ("expires_at", "timestamp with time zone"), ("used_at", "timestamp with time zone"),
                ("revoked_at", "timestamp with time zone")),
            ["email_outbox_messages"] = Columns(
                ("id", "uuid"), ("template_code", "character varying"), ("recipient_hash", "character varying"),
                ("protected_recipient", "text"), ("status", "character varying"), ("attempts", "integer"),
                ("next_attempt_at", "timestamp with time zone"), ("idempotency_key", "character varying"))
        };

    public async Task<DatabaseSchemaContractResult> CheckRegistrationContractAsync(CancellationToken ct = default)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            return Failed("database", null, "ConnectionNotConfigured");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        var columns = (await connection.QueryAsync<ColumnInfo>(new CommandDefinition("""
            SELECT table_name AS TableName, column_name AS ColumnName, data_type AS DataType
              FROM information_schema.columns
             WHERE table_schema = 'orcafacil'
               AND table_name = ANY(@Tables)
            """, new { Tables = RegistrationContract.Keys.ToArray() }, cancellationToken: ct))).AsList();

        var actual = columns.ToDictionary(x => $"{x.TableName}.{x.ColumnName}", StringComparer.OrdinalIgnoreCase);
        var issues = new List<DatabaseSchemaContractIssue>();
        foreach (var (table, expectedColumns) in RegistrationContract)
        foreach (var (column, type) in expectedColumns)
        {
            if (!actual.TryGetValue($"{table}.{column}", out var found))
                issues.Add(new(table, column, "Missing", RecommendedMigrationFor(table)));
            else if (!string.Equals(found.DataType, type, StringComparison.OrdinalIgnoreCase))
                issues.Add(new(table, column, $"TypeMismatch ({found.DataType})", RecommendedMigrationFor(table)));
        }

        await CheckEssentialObjectAsync(connection, issues, "billing_customer_profiles", "uq_billing_profiles_account_id", "IndexMissing", ct);
        await CheckEssentialObjectAsync(connection, issues, "account_members", "account_members_account_id_fkey", "ConstraintMissing", ct, constraint: true);
        foreach (var expected in EssentialIndexes)
            await CheckEssentialObjectAsync(connection, issues, expected.Table, expected.Name, "IndexMissing", ct);
        foreach (var expected in EssentialConstraints)
            await CheckEssentialObjectAsync(connection, issues, expected.Table, expected.Name, "ConstraintMissing", ct, constraint: true);

        var historySchema = await connection.ExecuteScalarAsync<string?>(new CommandDefinition("""
            select table_schema from information_schema.tables
             where table_name = '__EFMigrationsHistory' and table_schema in ('orcafacil', 'public')
             order by case when table_schema = 'orcafacil' then 0 else 1 end limit 1
            """, cancellationToken: ct));
        var trustedHistorySchema = historySchema == "orcafacil" ? "orcafacil" : historySchema == "public" ? "public" : null;
        HashSet<string> appliedMigrations = trustedHistorySchema is null
            ? []
            : (await connection.QueryAsync<string>(new CommandDefinition(
                $"select \"MigrationId\" from \"{trustedHistorySchema}\".\"__EFMigrationsHistory\" where \"MigrationId\" = ANY(@Migrations)",
                new { Migrations = RequiredMigrations }, cancellationToken: ct))).ToHashSet(StringComparer.Ordinal);
        foreach (var migration in RequiredMigrations.Where(x => !appliedMigrations.Contains(x)))
            issues.Add(new("__EFMigrationsHistory", null, "MigrationMissing", migration));
        var pending = appliedMigrations.Count != RequiredMigrations.Length;
        return new(issues.Count == 0 && !pending, issues, DateTimeOffset.UtcNow, pending);
    }

    public static readonly string[] RequiredMigrations = [RepairMigration, PasswordRecoveryMigration, CommercialJourneyMigration, QuoteToCashMigration, ReceiptSequenceMigration, CommercialDocumentRepairMigration, ClientsAndDocumentsDriftMigration, DepositAmountDriftMigration, CommercialSchemaV57Migration, DocumentsRowVersionV60Migration, DocumentsFullSchemaV61Migration, QualityGateSchemaDriftV62Migration];

    private static readonly (string Table, string Name)[] EssentialIndexes =
    [
        ("clients", "ix_clients_account_active"),
        ("clients", "ix_clients_account_name"),
        ("documents", "ix_documents_account_type_created"),
        ("documents", "ix_documents_account_type_followup"),
        ("documents", "ix_documents_account_type_valid_until"),
        ("documents", "ix_documents_public_token"),
        ("documents", "ix_documents_template_code"),
        ("budget_templates", "ix_budget_templates_account_active"),
        ("budget_templates", "ix_budget_templates_user_active"),
        ("budget_templates", "ix_budget_templates_system_active"),
        ("document_revisions", "ux_document_revisions_version"),
        ("document_revisions", "ux_document_revisions_current"),
        ("public_document_accesses", "ux_public_document_access_token_hash"),
        ("public_document_decisions", "ux_public_decision_revision"),
        ("public_document_decisions", "ux_public_decision_idempotency"),
        ("work_orders", "ux_work_orders_revision"),
        ("manual_payments", "ux_manual_payments_idempotency"),
        ("receipts", "ux_receipts_payment"),
        ("receipt_sequences", "ux_receipt_sequences_account_year"),
        ("password_reset_tokens", "uq_password_reset_tokens_token_hash"),
        ("email_outbox_messages", "uq_email_outbox_idempotency_key")
    ];

    private static readonly (string Table, string Name)[] EssentialConstraints =
    [
        ("document_revisions", "document_revisions_document_id_fkey"),
        ("public_document_accesses", "public_document_accesses_document_revision_id_fkey"),
        ("password_reset_tokens", "password_reset_tokens_user_id_fkey")
    ];

    private static async Task CheckEssentialObjectAsync(NpgsqlConnection connection, List<DatabaseSchemaContractIssue> issues,
        string table, string name, string state, CancellationToken ct, bool constraint = false)
    {
        var sql = constraint
            ? "select exists(select 1 from information_schema.table_constraints where table_schema='orcafacil' and table_name=@Table and constraint_name=@Name)"
            : "select exists(select 1 from pg_indexes where schemaname='orcafacil' and tablename=@Table and indexname=@Name)";
        if (!await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { Table = table, Name = name }, cancellationToken: ct)))
            issues.Add(new(table, null, state, RepairMigration));
    }

    private static IReadOnlyDictionary<string, string> Columns(params (string Name, string Type)[] columns) =>
        columns.ToDictionary(x => x.Name, x => x.Type, StringComparer.OrdinalIgnoreCase);

    private static string RecommendedMigrationFor(string table) => table switch
    {
        "password_reset_tokens" or "email_outbox_messages" => PasswordRecoveryMigration,
        "document_revisions" or "public_document_accesses" or "public_document_decisions" or
            "commercial_follow_ups" or "work_orders" => CommercialJourneyMigration,
        "receipt_sequences" => ReceiptSequenceMigration,
        "documents" or "budget_templates" or "budget_template_items" => QualityGateSchemaDriftV62Migration,
        "clients" => ClientsAndDocumentsDriftMigration,
        "manual_payments" or "receipts" => QuoteToCashMigration,
        _ => RepairMigration
    };

    private static DatabaseSchemaContractResult Failed(string table, string? column, string state) =>
        new(false, [new(table, column, state, RepairMigration)], DateTimeOffset.UtcNow, true);

    private sealed record ColumnInfo(string TableName, string ColumnName, string DataType);
}
