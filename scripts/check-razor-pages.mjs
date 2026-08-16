import { readdir, readFile } from 'node:fs/promises';
import { join, relative } from 'node:path';

const root = process.cwd();
const pages = join(root, 'src', 'OrcaFacil.Web');
async function files(dir) {
  const result = [];
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    if (['bin', 'obj'].includes(entry.name)) continue;
    const path = join(dir, entry.name);
    entry.isDirectory() ? result.push(...await files(path)) : result.push(path);
  }
  return result;
}
const errors = [];
for (const file of (await files(pages)).filter(path => path.endsWith('.cshtml'))) {
  const source = await readFile(file, 'utf8');
  if (/\bIndexModel\./.test(source)) errors.push(`${relative(root, file)}: referência estática IndexModel.`);
  if (/\bModel\.\w+Model\./.test(source)) errors.push(`${relative(root, file)}: membro de PageModel acessado como estático.`);
}
if (errors.length) { console.error(errors.join('\n')); process.exit(1); }
console.log('Razor Pages: referências de PageModel validadas (rotas e controles são verificados por check:routes).');
