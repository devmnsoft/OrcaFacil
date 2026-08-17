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
  const displayPath = relative(root, file);
  const lineOf = index => source.slice(0, index).split('\n').length;

  if (/\bIndexModel\./.test(source)) errors.push(`${displayPath}: referência estática IndexModel.`);
  if (/\bModel\.\w+Model\./.test(source)) errors.push(`${displayPath}: membro de PageModel acessado como estático.`);

  for (const match of source.matchAll(/<form\b[^>]*>/gi)) {
    if (!/\bmethod\s*=\s*["'](?:get|post|dialog)["']/i.test(match[0])) {
      errors.push(`${displayPath}:${lineOf(match.index)}: form sem method explícito get/post/dialog.`);
    }
  }

  for (const match of source.matchAll(/<button\b[^>]*>/gi)) {
    if (!/\btype\s*=\s*["'](?:button|submit|reset)["']/i.test(match[0])) {
      errors.push(`${displayPath}:${lineOf(match.index)}: button sem type explícito.`);
    }
  }

  for (const match of source.matchAll(/<a\b[^>]*\btarget\s*=\s*["']_blank["'][^>]*>/gi)) {
    if (!/\brel\s*=\s*["'][^"']*\bnoopener\b[^"']*["']/i.test(match[0])) {
      errors.push(`${displayPath}:${lineOf(match.index)}: link em nova aba sem rel="noopener".`);
    }
  }
}
if (errors.length) { console.error(errors.join('\n')); process.exit(1); }
console.log('Razor Pages: PageModels, forms, buttons e links em nova aba validados.');
