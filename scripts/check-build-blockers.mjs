import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';

const roots = ['src/OrcaFacil.Web', 'src/OrcaFacil.Application', 'src/OrcaFacil.Persistence', 'tests'];
const extensions = new Set(['.cs', '.cshtml', '.js']);
const ignored = new Set(['bin', 'obj', 'Migrations', 'node_modules']);

async function walk(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  return (await Promise.all(entries.map(entry => {
    if (ignored.has(entry.name)) return [];
    const target = path.join(directory, entry.name);
    return entry.isDirectory() ? walk(target) : [target];
  }))).flat();
}

const rules = [
  ['expressão `is var` combinada com `&&`; use uma variável explícita', /\bis\s+var\s+\w+\s*&&/],
  ['resultado de `Array.IndexOf` combinado com `&&`; use uma variável explícita', /Array\.IndexOf\([^\r\n]*\)[^\r\n]*&&/],
  ['implementação incompleta', /\bNotImplementedException\b/],
  ['link sem destino', /href\s*=\s*["']#["']/],
  ['link com destino vazio', /href\s*=\s*["']\s*["']/],
  ['URL JavaScript inerte', /javascript\s*:\s*void\b/i]
];
const failures = [];

for (const root of roots) {
  for (const file of (await walk(root)).filter(candidate => extensions.has(path.extname(candidate)))) {
    const lines = (await readFile(file, 'utf8')).split(/\r?\n/);
    lines.forEach((line, index) => {
      for (const [message, pattern] of rules) {
        if (pattern.test(line)) failures.push(`${file}:${index + 1}: ${message}`);
      }
      if (/\bbootstrap\s*\./.test(line) && !/(?:window\?\.|globalThis\?\.)bootstrap|typeof\s+bootstrap/.test(line)) {
        failures.push(`${file}:${index + 1}: Bootstrap global sem fallback`);
      }
    });
  }
}

if (failures.length) {
  console.error(`Bloqueadores de build e navegação encontrados:\n${failures.join('\n')}`);
  process.exit(1);
}
console.log('Build blockers: nenhum padrão de risco encontrado.');
