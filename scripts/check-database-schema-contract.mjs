import { readFile } from "node:fs/promises";

const file = "src/OrcaFacil.Persistence/Diagnostics/DatabaseSchemaContractService.cs";
const source = await readFile(file, "utf8");
const failures = [];
const requireText = (text, message) => { if (!source.includes(text)) failures.push(message); };
const section = table => {
  const startToken = `["${table}"] = Columns`;
  const start = source.indexOf(startToken);
  if (start < 0) return "";
  const end = source.indexOf('\n            ["', start + startToken.length);
  return source.slice(start, end < 0 ? source.length : end);
};

const criticalTables = [
  "users", "account_members", "business_accounts", "issuer_profiles", "clients", "contacts",
  "service_catalog_items", "documents", "document_items", "document_revisions", "budget_templates",
  "budget_template_items", "audit_logs", "account_onboarding_states", "email_outbox_messages", "plans",
  "plan_versions", "features", "plan_feature_values", "subscriptions", "notifications"
];
for (const table of criticalTables) {
  const contract = section(table);
  if (!contract) failures.push(`Missing schema contract table: ${table}`);
  else if (!contract.includes('("id",')) failures.push(`Schema contract table has no real columns: ${table}`);
}

const documentColumns = [
  "id", "account_id", "user_id", "number", "type", "status", "subtotal", "discount", "total", "issue_date",
  "valid_until", "client_id", "client_name", "client_document", "client_email", "client_phone", "client_city",
  "client_snapshot", "template_code", "template_snapshot", "conditions_text", "payment_method", "pix_information",
  "deposit_amount", "installment_count", "estimated_duration", "expected_start_at", "warranty_text", "evidence_hash",
  "follow_up_status", "follow_up_note", "last_follow_up_at", "next_follow_up_at", "current_wizard_step",
  "last_autosave_key", "last_autosaved_at", "public_enabled", "public_token", "client_decision",
  "client_decision_at", "client_decision_note", "internal_approval_status", "requires_internal_approval",
  "converted_receipt_id", "converted_receipt_number", "origin_budget_id", "origin_budget_number", "assigned_team_id",
  "assigned_to_user_id", "business_unit_id", "row_version", "created_at", "updated_at", "deleted_at", "deleted_by",
  "is_deleted"
];
const budgetTemplateColumns = [
  "id", "account_id", "user_id", "title", "profession", "is_system_template", "is_active", "is_deleted",
  "created_at", "updated_at", "deleted_at", "deleted_by"
];
for (const [table, columns] of [["documents", documentColumns], ["budget_templates", budgetTemplateColumns]]) {
  const contract = section(table);
  for (const column of columns) {
    if (!contract.includes(`("${column}",`)) failures.push(`Missing schema contract column: ${table}.${column}`);
  }
}

for (const marker of ["new NpgsqlConnection", "OpenAsync(ct)", "information_schema.columns", "RequiredMigrations", "__EFMigrationsHistory"]) {
  requireText(marker, `Database schema contract is not performing its real database check (${marker}).`);
}
if (/CheckRegistrationContractAsync[\s\S]{0,300}(?:return\s+Task\.FromResult)?\s*\(?new\s*\(\s*true\s*,\s*\[\s*\]/.test(source))
  failures.push("Database schema contract appears to return an unconditional empty success result.");

if (failures.length) {
  console.error(failures.join("\n"));
  process.exit(1);
}
console.log("Real database schema contract check passed.");
