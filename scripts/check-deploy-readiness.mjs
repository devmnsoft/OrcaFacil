import { existsSync, readFileSync } from 'node:fs';
const required=['DEPLOY-IIS.md','src/OrcaFacil.Web/web.config','src/OrcaFacil.Web/appsettings.Staging.json','src/OrcaFacil.Web/appsettings.Production.json','.env.example',...['setup-local','update-database','publish-release','install-iis','backup-db','restore-db','seed-superadmin','check-environment','start-local','stop-local'].map(x=>`scripts/windows/${x}.ps1`)];
const missing=required.filter(x=>!existsSync(x)); if(missing.length){console.error('Artefatos ausentes:\n'+missing.join('\n'));process.exit(1)}
const production=readFileSync('src/OrcaFacil.Web/appsettings.Production.json','utf8');
if(/Password=(?!CHANGE_ME)|postgres(?:ql)?:\/\//i.test(production)){console.error('Possível segredo em appsettings.Production.json');process.exit(1)}
for(const token of ['AspNetCoreModuleV2','OrcaFacil.Web.dll','DataProtection','SystemHealth','Uploads']) if(![readFileSync('src/OrcaFacil.Web/web.config','utf8'),production].some(x=>x.includes(token))){console.error(`Configuração ausente: ${token}`);process.exit(1)}
console.log('Deploy readiness: IIS, ambientes, operação e ausência de segredos versionados OK.');
