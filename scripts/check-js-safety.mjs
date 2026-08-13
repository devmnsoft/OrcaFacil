import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';

const root = 'src/OrcaFacil.Web';
async function walk(dir) { return (await readdir(dir, { withFileTypes: true })).flatMap(entry => entry.isDirectory() ? [] : [path.join(dir, entry.name)]).concat(...await Promise.all((await readdir(dir, { withFileTypes: true })).filter(entry => entry.isDirectory()).map(entry => walk(path.join(dir, entry.name))))); }
const files = await walk(root);
const failures = [];
for (const file of files.filter(file => /\.(?:js|cshtml)$/.test(file))) {
  const source = await readFile(file, 'utf8');
  if (/\bdata-bs-/.test(source)) failures.push(`${file}: Bootstrap data API`);
  if (/\bbootstrap\s*\./.test(source)) failures.push(`${file}: Bootstrap JavaScript global`);
  if (/href\s*=\s*["'](?:|#)["']/.test(source)) failures.push(`${file}: link vazio`);
  if (/querySelector\(\s*(?:\w+\.)?hash\s*\)/.test(source)) failures.push(`${file}: seletor hash bruto potencialmente inseguro`);
}
const layouts = files.filter(file => file.endsWith('.cshtml'));
for (const file of layouts) {
  const html = await readFile(file, 'utf8');
  for (const match of html.matchAll(/<script\b([^>]*)src=["']~?\/([^"']+\.js)["'][^>]*>/g)) {
    const jsPath = path.join(root, 'wwwroot', match[2]);
    let js; try { js = await readFile(jsPath, 'utf8'); } catch { continue; }
    if (/^\s*(?:import|export)\s/m.test(js) && !/type=["']module["']/.test(match[1])) failures.push(`${file}: ${match[2]} exige type="module"`);
  }
}
if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log(`Segurança JavaScript validada em ${files.length} arquivos.`);
