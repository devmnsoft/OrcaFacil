import { readdirSync, readFileSync, statSync } from 'node:fs';
function walk(dir){return readdirSync(dir).flatMap(name=>{const path=`${dir}/${name}`;return statSync(path).isDirectory()?walk(path):[path]})}
for(const file of walk('src/OrcaFacil.Web/wwwroot/js').filter(x=>x.endsWith('.js'))){const source=readFileSync(file,'utf8');if(source.includes('Math.random'))throw new Error(`${file}: Math.random proibido`);if(/javascript:void/i.test(source))throw new Error(`${file}: javascript:void proibido`)}
console.log('javascript safety: OK');
