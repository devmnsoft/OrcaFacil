import { read, requireAll, forbid, ok } from './schema-drift-check-utils.mjs';
const page = read('src/OrcaFacil.Web/Pages/Dashboard/Index.cshtml');
requireAll(page, ['of-workbench-intro', 'of-metric-grid', 'of-metrics-empty', 'of-quick-actions'], 'Dashboard');
forbid(page, [/Math\.random/i, /href=["']#["']/i, /javascript:void/i], 'Dashboard');
ok('dashboard premium sem dados ou links artificiais');
