import { read, requireAll, ok } from './schema-drift-check-utils.mjs';
for (const file of ['database/script_completop.sql', 'database/patch_release_candidate_schema.sql']) {
  const sql = read(file);
  requireAll(sql, ['orcafacil.clients', 'ADD COLUMN IF NOT EXISTS is_active', 'ADD COLUMN IF NOT EXISTS is_deleted', 'ix_clients_account_active', 'ix_clients_account_name'], file);
}
const contract = read('src/OrcaFacil.Persistence/Diagnostics/DatabaseSchemaContractService.cs');
requireAll(contract, ['("is_active", "boolean")', '("is_deleted", "boolean")'], 'contrato clients');
ok('schema de clients é idempotente e diagnosticado');
