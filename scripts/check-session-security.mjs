import { requireFiles } from './check-sprint14-common.mjs';
requireFiles('Sessões', ['src/OrcaFacil.Application/Security/SessionSecurityService.cs','src/OrcaFacil.Domain/Entities/PrivacyGovernance.cs'], ['canManageAccountSessions','SessionHash','RevokedAt']);
