import { requireFiles } from './check-sprint14-common.mjs';
requireFiles('Acesso sensível', ['src/OrcaFacil.Application/Security/SensitiveDataAccessService.cs','src/OrcaFacil.Domain/Entities/PrivacyGovernance.cs'], ['não pode conter o valor','CorrelationId']);
