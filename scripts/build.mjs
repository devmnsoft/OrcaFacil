import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import './copy-static.mjs';

const prod = process.argv.includes('--prod');
const root = process.cwd();
const pub = path.join(root, 'public');
const dist = path.join(root, 'dist');
const hash = (s) => crypto.createHash('sha256').update(s).digest('hex').slice(0, 8);
const ensure = (p) => fs.mkdir(p, { recursive: true });

function minifyJs(code) {
  let out = code.replace(/\/\*[\s\S]*?\*\//g, '').replace(/(^|[^:])\/\/.*$/gm, '$1');
  if (prod) out = out.replace(/console\.debug\([^;]*\);?/g, '');
  return out.replace(/\s+/g, ' ').replace(/\s*([{}()[\];,:?+*/%<>=|&!-])\s*/g, '$1').trim();
}
function minifyCss(code) { return code.replace(/\/\*[\s\S]*?\*\//g, '').replace(/\s+/g, ' ').replace(/\s*([{}:;,>])\s*/g, '$1').trim(); }
async function walk(dir) {
  const files = [];
  for (const e of await fs.readdir(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) files.push(...await walk(p)); else files.push(p);
  }
  return files;
}
async function processJs() {
  const srcDir = path.join(pub, 'js');
  const files = await walk(srcDir);
  const renamed = new Map();
  for (const file of files) {
    const rel = path.relative(srcDir, file).replace(/\\/g, '/');
    let code = minifyJs(await fs.readFile(file, 'utf8'));
    code = code.replace(/__IS_PRODUCTION_BUILD__/g, prod ? 'true' : 'false');
    if (prod && ['app.js', 'public-approval.js', 'diagnostico.js'].includes(rel)) {
      const name = rel.replace(/\.js$/, `.${hash(code)}.js`);
      renamed.set(`./js/${rel}`, `./js/${name}`);
      await ensure(path.dirname(path.join(dist, 'js', name)));
      await fs.writeFile(path.join(dist, 'js', name), code);
    } else {
      await ensure(path.dirname(path.join(dist, 'js', rel)));
      await fs.writeFile(path.join(dist, 'js', rel), code);
    }
  }
  return renamed;
}
async function processCss() {
  const cssPath = path.join(dist, 'css', 'app.css');
  try {
    const min = minifyCss(await fs.readFile(cssPath, 'utf8'));
    if (prod) {
      const name = `app.${hash(min)}.css`;
      await fs.writeFile(path.join(dist, 'css', name), min);
      await fs.rm(cssPath, { force: true });
      return new Map([['./css/app.css', `./css/${name}`]]);
    }
    await fs.writeFile(cssPath, min);
  } catch {}
  return new Map();
}
async function updateHtml(replacements) {
  for (const file of await walk(dist)) {
    if (!file.endsWith('.html')) continue;
    let html = await fs.readFile(file, 'utf8');
    for (const [from, to] of replacements) html = html.split(from).join(to);
    html = html.replace(/public<\/code> no Firebase Hosting/g, 'dist</code> no Firebase Hosting');
    if (prod) html = html.replace(/<!--[\s\S]*?-->/g, '').replace(/\n\s+/g, '\n');
    await fs.writeFile(file, html.trim() + '\n');
  }
}
const js = await processJs();
const css = await processCss();
await updateHtml(new Map([...js, ...css]));
const pkg = JSON.parse(await fs.readFile(path.join(root, 'package.json'), 'utf8'));
const versionInfo = { app: 'OrçaFácil', version: pkg.version, buildDate: new Date().toISOString(), commit: process.env.GITHUB_SHA || process.env.COMMIT_SHA || 'optional', environment: prod ? 'production' : 'development' };
await fs.writeFile(path.join(dist, 'version.json'), JSON.stringify(versionInfo, null, 2) + '\n');
console.log(`[build] dist gerada (${prod ? 'produção' : 'desenvolvimento'})`);
