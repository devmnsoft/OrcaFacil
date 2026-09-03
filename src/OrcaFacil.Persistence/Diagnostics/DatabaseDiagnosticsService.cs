using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Diagnostics;
using OrcaFacil.Application.Abstractions;

namespace OrcaFacil.Persistence.Diagnostics;

public sealed class DatabaseDiagnosticsService : IDatabaseDiagnosticsService
{
    private sealed class ColumnMetadata
    {
        public string ColumnName { get; init; } = string.Empty;
        public string DataType { get; init; } = string.Empty;
    }
    public const string ExpectedSchema = "orcafacil";

    public static readonly IReadOnlyList<string> RequiredTables =
    [
        "users", "issuer_profiles", "documents", "document_items", "document_revisions", "budget_templates", "budget_template_items", "public_quotes",
        "user_usage", "subscriptions", "payments", "payment_events", "mercadopago_webhook_events",
        "billing_customer_profiles", "clients", "contacts", "plan_features", "admin_settings", "notifications",
        "audit_logs", "system_logs", "system_errors", "business_accounts", "account_members",
        "plans", "plan_versions", "features", "plan_feature_values", "account_onboarding_states",
        "service_catalog_items", "work_orders", "manual_payments", "receipts", "payment_invoices", "payment_receipts",
        "email_outbox_messages", "commercial_follow_ups", "customer_success_accounts",
        "support_tickets", "support_ticket_messages", "user_feedback", "knowledge_base_articles", "release_notes",
        "recommendation_cards", "automation_rules", "automation_runs", "productivity_events",
        "file_assets", "file_asset_links", "company_branding_profiles", "document_templates",
        "document_template_versions", "document_audit_events", "privacy_consents", "data_subject_requests",
        "data_export_jobs", "data_retention_policies", "data_retention_runs", "sensitive_data_access_logs",
        "security_events", "session_records", "public_token_access_logs", "account_security_settings", "audit_export_jobs",
        "business_units", "business_unit_members", "teams", "team_members", "role_profiles", "role_profile_permissions",
        "discount_policies", "approval_requests", "approval_request_events", "white_label_settings",
        "unit_branding_profiles", "document_visibility_rules"
    ];

    public static readonly IReadOnlyDictionary<string, string> RequiredDocumentColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["row_version"] = "bytea", ["client_snapshot"] = "jsonb", ["template_code"] = "character varying", ["template_snapshot"] = "jsonb",
            ["conditions_text"] = "text", ["payment_method"] = "character varying", ["pix_information"] = "character varying",
            ["deposit_amount"] = "numeric", ["installment_count"] = "integer", ["estimated_duration"] = "character varying",
            ["expected_start_at"] = "timestamp with time zone", ["warranty_text"] = "character varying", ["evidence_hash"] = "character varying",
            ["follow_up_status"] = "character varying", ["follow_up_note"] = "character varying",
            ["last_follow_up_at"] = "timestamp with time zone", ["next_follow_up_at"] = "timestamp with time zone",
            ["current_wizard_step"] = "integer", ["last_autosave_key"] = "character varying",
            ["last_autosaved_at"] = "timestamp with time zone", ["public_enabled"] = "boolean", ["public_token"] = "character varying",
            ["client_decision"] = "character varying", ["client_decision_at"] = "timestamp with time zone",
            ["client_decision_note"] = "character varying", ["internal_approval_status"] = "character varying",
            ["requires_internal_approval"] = "boolean", ["converted_receipt_id"] = "uuid",
            ["converted_receipt_number"] = "character varying", ["origin_budget_id"] = "uuid",
            ["origin_budget_number"] = "character varying"
        };

    private readonly IConfiguration _configuration;

    public DatabaseDiagnosticsService(IConfiguration configuration) => _configuration = configuration;

    public async Task<bool> CanConnectForUserActionAsync(CancellationToken ct = default)
    {
        var result = await CheckAsync(ct);
        return result.CanConnect;
    }

    public async Task<DatabaseDiagnosticsDto> CheckAsync(CancellationToken ct = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new(false, false, [], RequiredTables, null, null, "Connection string DefaultConnection ausente.");
        }

        string? databaseName = null;
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            databaseName = builder.Database;

            await using var connection = new NpgsqlConnection(connectionString);
            var stopwatch = Stopwatch.StartNew();
            await connection.OpenAsync(ct);
            await connection.ExecuteScalarAsync<int>(new CommandDefinition("select 1", cancellationToken: ct));
            stopwatch.Stop();

            var schemaExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "select exists(select 1 from information_schema.schemata where schema_name = @Schema)",
                new { Schema = ExpectedSchema }, cancellationToken: ct));

            var existingTables = (await connection.QueryAsync<string>(new CommandDefinition(
                """
                select table_name
                  from information_schema.tables
                 where table_schema = @Schema
                   and table_type = 'BASE TABLE'
                 order by table_name
                """,
                new { Schema = ExpectedSchema }, cancellationToken: ct))).AsList();

            var existing = existingTables.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = RequiredTables.Where(table => !existing.Contains(table)).OrderBy(table => table).ToArray();

            var version = await connection.ExecuteScalarAsync<string>(new CommandDefinition(
                "select version()", cancellationToken: ct));
            var connectedUser = await connection.ExecuteScalarAsync<string>(new CommandDefinition("select current_user", cancellationToken: ct));
            var searchPath = await connection.ExecuteScalarAsync<string>(new CommandDefinition("show search_path", cancellationToken: ct));
            var canRead = await connection.ExecuteScalarAsync<bool>(new CommandDefinition("select has_schema_privilege(current_user, @Schema, 'USAGE')", new { Schema = ExpectedSchema }, cancellationToken: ct));
            var canWrite = await connection.ExecuteScalarAsync<bool>(new CommandDefinition("select has_schema_privilege(current_user, @Schema, 'CREATE')", new { Schema = ExpectedSchema }, cancellationToken: ct));
            // Keep authentication prerequisites explicit. A partially upgraded users table must be
            // diagnosed before EF Core attempts to materialize UserAccount during sign-in.
            var requiredColumns = new[]
            {
                "documents.account_id", "documents.row_version", "documents.client_snapshot", "documents.conditions_text", "documents.template_code", "documents.template_snapshot",
                "documents.follow_up_status", "documents.follow_up_note", "documents.last_follow_up_at",
                "documents.next_follow_up_at", "documents.current_wizard_step", "documents.last_autosave_key",
                "documents.last_autosaved_at", "documents.public_enabled", "documents.public_token",
                "documents.client_decision", "documents.client_decision_at", "documents.client_decision_note",
                "documents.internal_approval_status", "documents.requires_internal_approval",
                "documents.converted_receipt_id", "documents.converted_receipt_number",
                "documents.origin_budget_id", "documents.origin_budget_number", "documents.pix_information",
                "documents.evidence_hash", "documents.warranty_text", "documents.payment_method", "documents.deposit_amount",
                "documents.installment_count", "documents.estimated_duration", "documents.expected_start_at",
                "budget_templates.account_id", "budget_templates.user_id", "budget_templates.is_system_template",
                "budget_templates.is_active", "budget_templates.is_deleted",
                "email_outbox_messages.status",
                "clients.account_id", "clients.is_active", "clients.is_deleted", "public_document_accesses.token_hash",
                "users.failed_login_attempts", "users.last_failed_login_at",
                "users.last_successful_login_at", "users.locked_until", "users.is_blocked",
                "users.block_reason", "users.must_change_password", "users.password_changed_at",
                "users.password_changed_by_user_id", "users.password_expires_at",
                "users.password_reset_reason", "users.session_version", "users.accepted_terms_at",
                "users.accepted_privacy_at", "users.legacy_unversioned_acceptance",
                "account_onboarding_states.id", "account_onboarding_states.account_id",
                "account_onboarding_states.user_id", "account_onboarding_states.current_step",
                "account_onboarding_states.completed_at", "account_onboarding_states.skipped_at",
                "account_onboarding_states.last_seen_at", "account_onboarding_states.created_at",
                "account_onboarding_states.updated_at", "account_onboarding_states.is_deleted",
                "work_orders.account_id", "work_orders.cancellation_reason",
                "work_order_checklist_items.account_id", "work_order_checklist_items.work_order_id",
                "work_order_checklist_items.is_required", "work_order_checklist_items.is_completed",
                "support_tickets.related_page", "support_tickets.correlation_id", "support_tickets.browser_info",
                "user_feedback.page_url", "user_feedback.rating", "recommendation_cards.account_id",
                "automation_rules.account_id", "productivity_events.account_id", "knowledge_base_articles.slug",
                "knowledge_base_articles.is_published", "release_notes.version", "release_notes.is_published",
                "audit_logs.summary", "audit_logs.correlation_id", "data_subject_requests.client_id",
                "data_subject_requests.resolution_notes", "data_subject_requests.reviewed_at",
                "privacy_consents.account_id", "session_records.session_hash", "sensitive_data_access_logs.correlation_id",
                "documents.business_unit_id", "documents.assigned_to_user_id", "documents.assigned_team_id",
                "documents.requires_internal_approval", "approval_requests.account_id", "business_units.account_id"
            };
            var columns = (await connection.QueryAsync<string>(new CommandDefinition("select table_name || '.' || column_name from information_schema.columns where table_schema=@Schema", new { Schema = ExpectedSchema }, cancellationToken: ct))).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingColumns = requiredColumns.Where(x => !columns.Contains(x)).ToArray();
            var requiredIndexes = new[]
            {
                "ix_documents_account_client", "ix_clients_account_active", "ix_clients_account_name", "ix_documents_account_type_created", "ix_documents_account_type_followup",
                "ix_documents_account_type_valid_until", "ix_documents_public_token", "ix_documents_template_code",
                "ux_public_document_access_token_hash", "ix_work_orders_schedule",
                "ix_account_onboarding_states_account_id_user_id",
                "ix_account_onboarding_states_current_step_last_seen_at",
                "ix_recommendation_cards_account_status_priority", "ix_productivity_events_account_occurred",
                "ix_privacy_consents_account_user", "ix_sensitive_access_account_entity", "ix_sessions_account_user",
                "ix_business_units_account", "ix_approval_queue", "ix_documents_enterprise_scope"
            };
            var indexes = (await connection.QueryAsync<string>(new CommandDefinition("select indexname from pg_indexes where schemaname=@Schema", new { Schema = ExpectedSchema }, cancellationToken: ct))).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingIndexes = requiredIndexes.Where(x => !indexes.Contains(x)).ToArray();
            var documentTypes = (await connection.QueryAsync<ColumnMetadata>(new CommandDefinition(
                "select column_name as ColumnName, data_type as DataType from information_schema.columns where table_schema=@Schema and table_name='documents'",
                new { Schema = ExpectedSchema }, cancellationToken: ct))).ToDictionary(x => x.ColumnName, x => x.DataType, StringComparer.OrdinalIgnoreCase);
            var driftIssues = new List<SchemaDriftIssue>();
            foreach (var table in missing)
                driftIssues.Add(new(table, "MissingTable", "Critical", "Database", ["/SystemHealth"], "database/script_completop.sql"));
            foreach (var expected in RequiredDocumentColumns)
            {
                if (!documentTypes.TryGetValue(expected.Key, out var actual))
                    driftIssues.Add(new($"documents.{expected.Key}", "MissingColumn", "Critical", "Commercial", ["/Dashboard", "/Documents/New", "/CommercialRoutine"], "database/hotfix_documents_full_schema_drift_v61.sql", expected.Value));
                else if (!string.Equals(actual, expected.Value, StringComparison.OrdinalIgnoreCase))
                    driftIssues.Add(new($"documents.{expected.Key}", "IncompatibleType", "Critical", "Commercial", ["/Dashboard", "/Documents/New", "/CommercialRoutine"], "database/hotfix_documents_full_schema_drift_v61.sql", expected.Value, actual));
            }
            foreach (var index in missingIndexes)
                driftIssues.Add(new(index, "MissingIndex", "Warning", "Database", ["/SystemHealth"], "database/hotfix_documents_full_schema_drift_v61.sql"));
            var appliedMigrations = existing.Contains("__EFMigrationsHistory")
                ? (await connection.QueryAsync<string>(new CommandDefinition("select migration_id from orcafacil.\"__EFMigrationsHistory\" order by migration_id", cancellationToken: ct))).ToArray()
                : Array.Empty<string>();

            var freePlan = missing.Length == 0 && await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "select exists(select 1 from orcafacil.plans where code='FREE' and is_active and not is_deleted)", cancellationToken: ct));
            var publishedFreeVersion = freePlan && await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                """
                select exists(select 1 from orcafacil.plan_versions pv join orcafacil.plans p on p.id=pv.plan_id
                 where p.code='FREE' and pv.status='Published' and not pv.is_deleted
                   and pv.valid_from <= now() and (pv.valid_until is null or pv.valid_until > now()))
                """, cancellationToken: ct));

            return new(true, schemaExists, existingTables, missing, databaseName, version, null, freePlan, publishedFreeVersion,
                missingColumns, missingIndexes, connectedUser, searchPath, stopwatch.ElapsedMilliseconds, canRead, canWrite, appliedMigrations, driftIssues);
        }
        catch (Exception ex)
        {
            return new(false, false, [], RequiredTables, databaseName, null, SanitizeError(ex));
        }
    }

    public static string MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return string.Empty;
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.Password)) builder.Password = "******";
        return builder.ConnectionString;
    }

    private static string SanitizeError(Exception ex)
    {
        if (HasSqlState(ex, "28P01"))
        {
            return PostgresFailureClassifier.Classify(ex).AdminMessage;
        }

        return PostgresFailureClassifier.Classify(ex).AdminMessage;
    }

    private static bool HasSqlState(Exception ex, string sqlState)
    {
        for (var current = ex; current is not null; current = current.InnerException!)
        {
            var value = current.GetType().GetProperty("SqlState")?.GetValue(current)?.ToString();
            if (string.Equals(value, sqlState, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
