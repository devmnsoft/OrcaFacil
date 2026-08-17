import { readdir, readFile } from 'node:fs/promises';
import path from 'node:path';

const root = 'src/OrcaFacil.Web/Areas/Admin/Pages';
async function files(dir) {
  return (await readdir(dir, { withFileTypes: true })).flatMap(entry =>
    entry.isDirectory() ? [] : [path.join(dir, entry.name)]);
}
async function walk(dir) {
  const entries = await readdir(dir, { withFileTypes: true });
  const nested = await Promise.all(entries.map(e => e.isDirectory() ? walk(path.join(dir, e.name)) : [path.join(dir, e.name)]));
  return nested.flat();
}

const pages = (await walk(root)).filter(f => f.endsWith('.cshtml') && !path.basename(f).startsWith('_'));
const failures = [];
for (const page of pages) {
  const view = await readFile(page, 'utf8');
  const modelFile = `${page}.cs`;
  let model = '';
  try { model = await readFile(modelFile, 'utf8'); } catch { /* authorization may live in Razor */ }
  if (!/Authorize\s*\(\s*Policy\s*=\s*"(?:SuperAdmin|SuperAdminOnly|Platform)/.test(`${view}\n${model}`))
    failures.push(`${page}: rota administrativa sem policy de plataforma`);
  if (/<form\b[^>]*method\s*=\s*["']post["'][^>]*>/i.test(view) && /asp-antiforgery\s*=\s*["']false["']/i.test(view))
    failures.push(`${page}: POST desativou antiforgery`);
}
if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log(`Segurança administrativa validada em ${pages.length} páginas.`);
