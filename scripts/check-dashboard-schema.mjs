import { read, requireAll, ok } from './schema-drift-check-utils.mjs';
const source = read('src/OrcaFacil.Persistence/Services/CommercialWorkspaceQueryService.cs');
requireAll(source, ['AsNoTracking()', 'x.AccountId == AccountId', 'document.ConditionsText'], 'dashboard comercial');
const contract = read('src/OrcaFacil.Persistence/Diagnostics/DatabaseSchemaContractService.cs');
requireAll(contract, ['("conditions_text", "text")', 'ix_documents_account_type_followup'], 'contrato documents');
ok('dashboard usa consulta tenant-safe e contrato de schema');
