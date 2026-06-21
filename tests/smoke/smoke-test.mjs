import fs from 'node:fs/promises';
import http from 'node:http';
let errors = 0;
const fail = (m) => { errors += 1; console.error(`ERRO: ${m}`); };
async function exists(p) { try { await fs.access(p); return true; } catch { return false; } }
const pkg = JSON.parse(await fs.readFile('package.json', 'utf8'));
const required = ['start','check','check:js','build:prod','security:check','validate','serve:dist','deploy:hosting','deploy:rules','deploy:functions','deploy'];
for (const script of required) if (!pkg.scripts?.[script]) fail(`script obrigatório ausente: ${script}`);
if (!await exists('dist/index.html')) fail('dist/index.html não existe; rode npm run build:prod.');
const distEntries = await fs.readdir('dist').catch(() => []);
if (!distEntries.includes('js') && !distEntries.includes('assets')) fail('dist não contém js ou assets.');
for (const name of ['.env', '.env.local', 'README.md']) if (await exists(`dist/${name}`)) fail(`dist contém ${name}.`);
async function walk(dir){ const out=[]; for(const e of await fs.readdir(dir,{withFileTypes:true}).catch(()=>[])){ const p=`${dir}/${e.name}`; if(e.isDirectory()) out.push(...await walk(p)); else out.push(p); } return out; }
for (const f of await walk('dist')) if (f.endsWith('.map')) fail(`source map em ${f}`);
const firebase = JSON.parse(await fs.readFile('firebase.json', 'utf8'));
if (firebase.hosting?.public !== 'dist') fail('firebase.json não aponta hosting.public para dist.');
await new Promise((resolve) => { const req = http.get('http://127.0.0.1:8095/health', (res) => { res.resume(); console.log(`health local respondeu HTTP ${res.statusCode}`); resolve(); }); req.on('error', () => { console.warn('AVISO: server.js não está rodando em /health; smoke continuou.'); resolve(); }); req.setTimeout(1000, () => { req.destroy(); resolve(); }); });
console.log(`smoke-test: ${errors} erro(s)`);
process.exit(errors ? 1 : 0);
