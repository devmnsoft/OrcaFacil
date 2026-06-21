import fs from 'node:fs/promises';
import path from 'node:path';
const dist = path.resolve('dist');
const forbiddenNames = ['.env','.env.local','.env.production','README.md','ARCHITECTURE.md','scripts','tests','functions','package.json'];
const forbiddenContent = ['serviceAccount','private_key','TELEGRAM_BOT_TOKEN','OPENAI_API_KEY','sourceMappingURL'];
let errors = 0, warnings = 0;
async function exists(p){try{await fs.access(p);return true;}catch{return false;}}
async function walk(dir){const out=[]; for(const e of await fs.readdir(dir,{withFileTypes:true})){const p=path.join(dir,e.name); if(e.isDirectory()) out.push(...await walk(p)); else out.push(p);} return out;}
function fail(m){errors++; console.error('ERRO:',m);} function warn(m){warnings++; console.warn('AVISO:',m);}
if(!await exists(dist)) fail('dist não existe. Rode npm run build:prod.');
else {
 const files=await walk(dist);
 for(const f of files){const rel=path.relative(dist,f).replace(/\\/g,'/'); if(forbiddenNames.some(n=>rel===n||rel.startsWith(`${n}/`)||rel.endsWith(`/${n}`))) fail(`arquivo proibido em dist: ${rel}`); if(rel.endsWith('.map')) fail(`source map encontrado: ${rel}`); const txt=await fs.readFile(f,'utf8').catch(()=>null); if(txt){ for(const p of forbiddenContent) if(txt.includes(p)) fail(`padrão sensível ${p} em ${rel}`); if(txt.includes('AIzaSy')) warn(`Firebase Web apiKey encontrada em ${rel}; é pública, confirme Rules/App Check.`); }}
 if(!files.some(f=>f.endsWith('web.config'))) fail('dist/web.config não encontrado.');
 const js=files.filter(f=>f.endsWith('.js')); if(!js.length) fail('nenhum JS em dist.');
 for(const f of js){const txt=await fs.readFile(f,'utf8'); if(txt.length>1000 && txt.includes('\n') && txt.split('\n').length>20) warn(`JS pode não estar minificado: ${path.relative(dist,f)}`);}
 const firebase=JSON.parse(await fs.readFile('firebase.json','utf8')); if(firebase.hosting?.public!=='dist') fail('firebase.json não aponta hosting.public para dist.');
}
console.log(`security-check: ${errors} erro(s), ${warnings} aviso(s)`); process.exit(errors?1:0);
