import { read, requireAll, ok } from './schema-drift-check-utils.mjs';
requireAll(read('src/OrcaFacil.Web/Pages/Onboarding/Index.cshtml'),['progress','cliente','serviço'], 'Onboarding');
ok('design de onboarding orientado por progresso real');
