import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';

const roots = [
  'src/OrcaFacil.Web',
  'src/OrcaFacil.Application',
  'src/OrcaFacil.Persistence'
];
const extensions = new Set(['.cs', '.cshtml', '.js']);
const ignoredDirectories = new Set(['bin', 'obj', 'Migrations', 'wwwroot']);

async function walk(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const nested = await Promise.all(entries.map(entry => {
    if (ignoredDirectories.has(entry.name)) return [];
    const target = path.join(directory, entry.name);
    return entry.isDirectory() ? walk(target) : [target];
  }));
  return nested.flat();
}

const rules = [
  ['marcador TODO/FIXME', /\b(?:TODO|FIXME)\b/],
  ['funcionalidade anunciada como futura', /\b(?:Em breve|Coming soon)\b/i],
  ['implementação não concluída', /\bNotImplementedException\b|throw\s+new\s+NotSupportedException\s*\(\s*\)/],
  ['interface simulada', /\b(?:fake|mock)\s*(?:button|data|screen|page|modal|action)\b/i],
  ['link sem destino', /href\s*=\s*["'](?:|#|javascript:void\s*\(\s*0\s*\))["']/i]
];

const failures = [];
const buttonWithoutType = /<button\b(?![^>]*\btype\s*=)[^>]*>/gi;
const disabledAntiforgery = /<form\b[^>]*\bmethod\s*=\s*["']post["'][^>]*\basp-antiforgery\s*=\s*["']false["'][^>]*>/gi;
for (const root of roots) {
  for (const file of (await walk(root)).filter(candidate => extensions.has(path.extname(candidate)))) {
    const lines = (await readFile(file, 'utf8')).split(/\r?\n/);
    lines.forEach((line, index) => {
      if (path.extname(file) === '.cshtml') {
        if (buttonWithoutType.test(line)) failures.push(`${file}:${index + 1}: botão sem atributo type`);
        buttonWithoutType.lastIndex = 0;
        if (disabledAntiforgery.test(line)) failures.push(`${file}:${index + 1}: formulário POST sem antiforgery`);
        disabledAntiforgery.lastIndex = 0;
      }
      for (const [description, pattern] of rules) {
        if (pattern.test(line)) failures.push(`${file}:${index + 1}: ${description}`);
      }
    });
  }
}

if (failures.length) {
  console.error(`Funcionalidades abertas ou controles inertes encontrados:\n${failures.join('\n')}`);
  process.exit(1);
}

console.log('Funcionalidades abertas: nenhum marcador, implementação pendente ou controle inerte encontrado.');
