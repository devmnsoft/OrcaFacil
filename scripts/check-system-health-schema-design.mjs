import { read, requireAll, ok } from './schema-drift-check-utils.mjs';
const diagnostics=read('src/OrcaFacil.Persistence/Diagnostics/DatabaseSchemaContractService.cs');
requireAll(diagnostics,['documents','payment_method','budget_templates','account_id','CommercialSchemaV57Migration'], 'SystemHealth schema source');
ok('SystemHealth recebe diagnóstico preventivo do schema comercial');
