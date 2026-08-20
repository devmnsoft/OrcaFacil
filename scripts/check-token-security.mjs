import { requireFiles } from './check-sprint14-common.mjs';
requireFiles('Tokens públicos', ['src/OrcaFacil.Domain/Entities/PrivacyGovernance.cs','database/patch_release_candidate_schema.sql'], ['PublicTokenAccessLog','public_token_access_logs']);
