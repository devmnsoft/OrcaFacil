import { requirePaths } from './source-audit-lib.mjs';
await requirePaths(['src/OrcaFacil.Application/Quality/FunctionalQualityServices.cs','tests/OrcaFacil.UnitTests/FunctionalQualityServiceTests.cs'], 'business rule consistency');
