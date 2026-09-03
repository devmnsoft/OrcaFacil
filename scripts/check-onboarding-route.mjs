import { read, requireAll, ok } from './schema-drift-check-utils.mjs';
requireAll(read('src/OrcaFacil.Web/Pages/Onboarding/Index.cshtml'), ['of-activation-list','<progress','ActivationSteps'], 'Onboarding');
ok('rota de onboarding possui checklist e progresso orientados por dados');
