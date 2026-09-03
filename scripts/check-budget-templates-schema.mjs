import { read, requireAll, ok } from './schema-drift-check-utils.mjs';
const columns=['account_id','user_id','is_system_template','is_active','is_deleted'];
for (const file of ['database/hotfix_commercial_schema_drift_v57.sql','database/script_completop.sql','database/patch_release_candidate_schema.sql'])
  requireAll(read(file), ['orcafacil.budget_templates', ...columns], file);
requireAll(read('src/OrcaFacil.Persistence/Diagnostics/DatabaseSchemaContractService.cs'), ['["budget_templates"]', ...columns.map(x=>`("${x}"`)], 'schema diagnostics');
ok('budget_templates coberto no contrato multi-tenant e nos três caminhos SQL');
