import { readFileSync, existsSync } from 'node:fs';
const root = new URL('../', import.meta.url);
const required = [
  'src/OrcaFacil.Application/GoLive/GoLiveRules.cs', 'database/sprint32_go_live_v33.sql',
  'tests/OrcaFacil.UnitTests/GoLiveRulesTests.cs'
];
const missing = required.filter(path => !existsSync(new URL(path, root)));
if (missing.length) throw new Error(`Sprint 32: arquivos ausentes: ${missing.join(', ')}`);
const rules = readFileSync(new URL(required[0], root), 'utf8');
const schema = readFileSync(new URL(required[1], root), 'utf8');
for (const token of ['TenantProvisioningService', 'CustomerMigrationService', 'DemoAccountService', 'AccountReadinessService', 'GoLiveReviewService'])
  if (!rules.includes(token)) throw new Error(`Sprint 32: regra ausente: ${token}`);
for (const table of ['tenant_provisioning_requests', 'tenant_launch_checklists', 'demo_accounts', 'customer_migration_batches', 'customer_go_live_reviews'])
  if (!schema.includes(`CREATE TABLE IF NOT EXISTS orcafacil.${table}`)) throw new Error(`Sprint 32: tabela idempotente ausente: ${table}`);
if (/Math\.random|Random\.Shared/.test(rules)) throw new Error('Sprint 32: score não determinístico detectado.');
if (!/block_email boolean NOT NULL DEFAULT true/.test(schema) || !/block_webhook boolean NOT NULL DEFAULT true/.test(schema))
  throw new Error('Sprint 32: integrações demo não estão bloqueadas por padrão.');
console.log('Sprint 32: contratos críticos, isolamento e schema idempotente validados.');
