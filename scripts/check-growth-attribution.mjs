import { requireFiles } from './check-growth-v34-lib.mjs';
requireFiles('growth-attribution', ['database/sprint33_growth_v34.sql'], ['growth_utm_events', 'Direct/Unknown', 'gclid', 'fbclid']);
