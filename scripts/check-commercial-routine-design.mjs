import { read, requireAll, forbid, ok } from './schema-drift-check-utils.mjs';
const page=read('src/OrcaFacil.Web/Pages/CommercialRoutine/Index.cshtml');
requireAll(page,['Rotina Comercial','empty','follow'], 'CommercialRoutine');
forbid(page,[/Math\.random/i,/href=["']#["']/i], 'CommercialRoutine');
ok('design da rotina comercial baseado em dados reais');
