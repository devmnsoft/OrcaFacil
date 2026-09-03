import { read, requireAll, ok } from './schema-drift-check-utils.mjs';
requireAll(read('src/OrcaFacil.Web/wwwroot/css/design-system.css'), ['--','box-shadow','border-radius'], 'design system');
requireAll(read('src/OrcaFacil.Web/Pages/Documents/New.cshtml'), ['hero','empty'], 'Documents/New');
requireAll(read('src/OrcaFacil.Web/Pages/CommercialRoutine/Index.cshtml'), ['hero','empty'], 'CommercialRoutine');
ok('superfícies premium V5.7 verificadas');
