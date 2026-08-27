import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { extname, join, relative } from 'node:path';

const root = process.cwd();
const pagesRoot = join(root, 'src/OrcaFacil.Web/Pages');
const sourceRoots = [
  'src/OrcaFacil.Web',
  'src/OrcaFacil.Application',
  'src/OrcaFacil.Domain',
  'src/OrcaFacil.Persistence',
  'src/OrcaFacil.Infrastructure'
].map(path => join(root, path));

function filesBelow(directory, extensions) {
  if (!existsSync(directory)) return [];
  return readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) return filesBelow(path, extensions);
    return extensions.has(extname(entry.name)) ? [path] : [];
  });
}

const sourceFiles = sourceRoots.flatMap(path => filesBelow(path, new Set(['.cs', '.cshtml', '.js'])));
const razorFiles = filesBelow(pagesRoot, new Set(['.cshtml']));
const failures = [];

function scan(files, expression, message, predicate = () => true) {
  for (const file of files) {
    if (!predicate(file)) continue;
    const lines = readFileSync(file, 'utf8').split(/\r?\n/);
    lines.forEach((line, index) => {
      if (expression.test(line)) failures.push(`${relative(root, file)}:${index + 1}: ${message}`);
      expression.lastIndex = 0;
    });
  }
}

scan(sourceFiles, /\bthrow\s+new\s+NotImplementedException\b/, 'implementação pendente');
scan(sourceFiles, /\bMath\.random\s*\(/, 'métrica ou comportamento não pode usar dado aleatório');
scan(razorFiles, /href\s*=\s*["'](?:#|javascript:void(?:\([^)]*\))?)["']/i, 'link sem destino real');
scan(razorFiles, /<button\b(?![^>]*\btype\s*=)[^>]*>/i, 'button deve declarar type');

for (const file of razorFiles) {
  const content = readFileSync(file, 'utf8');
  const forms = content.match(/<form\b[\s\S]*?<\/form>/gi) ?? [];
  forms.forEach(form => {
    const optsOut = /asp-antiforgery\s*=\s*["']false["']/i.test(form);
    if (optsOut) {
      failures.push(`${relative(root, file)}: formulário desabilita antiforgery Razor`);
    }
  });
}

// Technical identifiers may be transported in hidden fields, but must never be
// presented as free-form input to an operator.
scan(
  razorFiles,
  /<(?:input|textarea)\b(?=[^>]*(?:name|asp-for)\s*=\s*["'][^"']*(?:Account|Client|Partner|User|Plan|Service)Id["'])(?![^>]*type\s*=\s*["']hidden["'])[^>]*>/i,
  'ID técnico exposto como entrada livre'
);

if (failures.length) {
  console.error(`Sprint 41: ${failures.length} bloqueio(s) funcional(is):\n${failures.join('\n')}`);
  process.exit(1);
}

console.log(`Sprint 41: ${sourceFiles.length} fontes e ${razorFiles.length} páginas verificadas sem bloqueios estáticos.`);
