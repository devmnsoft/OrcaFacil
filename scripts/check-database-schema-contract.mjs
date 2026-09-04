import { readFile } from "node:fs/promises";

const source = await readFile("src/OrcaFacil.Persistence/Diagnostics/DatabaseSchemaContractService.cs", "utf8");
const tables = ["users", "account_members", "business_accounts", "issuer_profiles", "clients", "contacts", "service_catalog_items", "documents", "document_items", "document_revisions", "budget_templates", "budget_template_items", "audit_logs", "account_onboarding_states", "email_outbox_messages", "plans", "plan_versions", "features", "plan_feature_values", "subscriptions", "notifications"];
const columns = ["row_version", "template_code", "payment_method", "account_id"];
const missing = [...tables, ...columns].filter(token => !source.includes(`\"${token}\"`));
if (!source.includes("information_schema.columns")) missing.push("live information_schema query");
if (/RegistrationContract\s*=\s*(?:\[\]|new\s+Dictionary[^;]*\{\s*\})/s.test(source)) missing.push("non-empty RegistrationContract");
if (missing.length) { console.error(`Missing real schema contract elements: ${missing.join(", ")}`); process.exit(1); }
console.log("Database schema contract contains all critical tables and columns and queries the live schema.");
