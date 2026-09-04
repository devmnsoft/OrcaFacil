import fs from 'node:fs'; import path from 'node:path';
const root=process.cwd(); const read=p=>fs.readFileSync(path.join(root,p),'utf8'); const exists=p=>fs.existsSync(path.join(root,p));
const required=[
'src/OrcaFacil.Domain/Entities/GoLiveOperations.cs','src/OrcaFacil.Application/GoLive/GoLiveV65Services.cs','src/OrcaFacil.Persistence/Services/GoLive/GoLivePersistenceService.cs','src/OrcaFacil.Persistence/Migrations/20260904000000_AddGoLiveControlV65.cs','src/OrcaFacil.Web/Pages/GoLive/Checklist.cshtml','src/OrcaFacil.Web/Pages/Admin/ProductionReadiness.cshtml','src/OrcaFacil.Web/Pages/Admin/AssistedOperation/Index.cshtml','src/OrcaFacil.Web/Pages/Training/Index.cshtml','src/OrcaFacil.Web/Pages/Shared/_ContextualHelp.cshtml','src/OrcaFacil.Web/Middleware/CriticalRouteMonitorMiddleware.cs'];
export function checkV65(scope='all'){
 const missing=required.filter(p=>!exists(p)); if(missing.length)throw new Error(`V6.5 incompleto (${scope}): ${missing.join(', ')}`);
 const domain=read(required[0]), app=read(required[1]), page=read(required[4]), middleware=read(required[9]);
 const contracts=['AccountId','IsAutomatic','IsCritical','ResponsibleName','Observation','ReadyForProduction','TrainingProgress','CorrelationId','SanitizedError'];
 for(const token of contracts)if(!(domain+app+page+middleware).includes(token))throw new Error(`Contrato ausente: ${token}`);
 if(!read(required[2]).includes('SaveChangesAsync')||!read(required[3]).includes('CREATE TABLE'))throw new Error('Checklist não possui persistência real.');
 const source=required.filter(p=>p.endsWith('.cshtml')).map(read).join('\n'); if(/href\s*=\s*["'](?:#|javascript:void|)["']/i.test(source))throw new Error('Link vazio detectado.');
 if(/Math\.random|\balert\s*\(|\bconfirm\s*\(/.test(domain+app+page+middleware))throw new Error('Implementação insegura ou dado aleatório detectado.');
 console.log(`check:${scope}: V6.5 validado (${required.length} contratos reais).`);
}
