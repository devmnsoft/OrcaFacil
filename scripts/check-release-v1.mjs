import { spawnSync } from 'node:child_process';
const checks=['check:database-schema','check:js-safety','check:public-navigation','check:authenticated-navigation','check:open-features','check:design-consistency','check:deploy-readiness'];
for(const check of checks){const r=spawnSync(process.platform==='win32'?'npm.cmd':'npm',['run',check],{stdio:'inherit'});if(r.status!==0)process.exit(r.status??1)}
console.log('Contrato estático da Release Candidate V1 aprovado.');
