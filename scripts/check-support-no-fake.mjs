import fs from 'node:fs'; import path from 'node:path';
const roots=['src/OrcaFacil.Web/Pages/Support','src/OrcaFacil.Web/Areas/Admin/Pages/Support','src/OrcaFacil.Web/Services/SupportDeskService.cs'];
const files=[]; const walk=p=>{const s=fs.statSync(p);if(s.isDirectory())for(const x of fs.readdirSync(p))walk(path.join(p,x));else files.push(p)}; roots.filter(fs.existsSync).forEach(walk);
const forbidden=[/Math\.random\s*\(/i,/javascript:void/i,/href\s*=\s*["']#["']/i,/NotImplementedException/i];const errors=[];for(const f of files){const t=fs.readFileSync(f,'utf8');for(const x of forbidden)if(x.test(t))errors.push(`${f}: ${x}`)}if(errors.length){console.error(errors.join('\n'));process.exit(1)}console.log('Service Desk: nenhuma implementação simulada detectada.');
