import { read, requireAll, forbid, ok } from './schema-drift-check-utils.mjs';
const page = read('src/OrcaFacil.Web/Pages/Documents/New.cshtml');
requireAll(page, ['of-budget-start-hero', 'of-start-options', 'of-start-search', 'of-start-empty', 'of-picker-list'], 'Documents/New');
forbid(page, [/Math\.random/i, /name=["'](?:Account|User|Document)Id/i, /href=["']#["']/i], 'Documents/New');
ok('novo documento guiado sem identificadores técnicos');
