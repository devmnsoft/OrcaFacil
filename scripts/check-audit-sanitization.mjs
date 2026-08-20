import { requireFiles } from './check-sprint14-common.mjs';
requireFiles('Sanitização de auditoria', ['src/OrcaFacil.Application/Security/SensitiveDataSanitizer.cs','src/OrcaFacil.Persistence/AuditService.cs'], ['connectionstring','SanitizeJson','CorrelationId']);
