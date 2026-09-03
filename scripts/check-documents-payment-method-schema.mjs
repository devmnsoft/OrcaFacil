import { read, requireAll, ok } from './schema-drift-check-utils.mjs';
for (const file of ['database/hotfix_commercial_schema_drift_v57.sql','database/script_completop.sql','database/patch_release_candidate_schema.sql'])
  requireAll(read(file), ['orcafacil.documents','payment_method'], file);
requireAll(read('src/OrcaFacil.Persistence/Diagnostics/DatabaseSchemaContractService.cs'), ['("payment_method", "character varying")'], 'schema diagnostics');
ok('documents.payment_method coberto no contrato e nos três caminhos SQL');
