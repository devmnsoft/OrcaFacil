import { readFileSync, readdirSync, statSync } from 'node:fs';
import { extname, join, relative } from 'node:path';

const root = process.cwd();
const scopes = ['src/OrcaFacil.Application', 'src/OrcaFacil.Domain', 'src/OrcaFacil.Persistence', 'src/OrcaFacil.Infrastructure', 'src/OrcaFacil.Web/Pages'];
const extensions = new Set(['.cs', '.cshtml', '.js']);
const findings = [];
function walk(path) {
  for (const name of readdirSync(path)) {
    const file = join(path, name);
    if (statSync(file).isDirectory()) { if (!['bin', 'obj'].includes(name)) walk(file); continue; }
    if (!extensions.has(extname(file))) continue;
    const source = readFileSync(file, 'utf8');
    const rel = relative(root, file).replaceAll('\\', '/');
    const rules = [
      [/throw\s+new\s+NotImplementedException/g, 'NotImplementedException'],
      [/catch\s*(?:\([^)]*\))?\s*\{\s*\}/g, 'catch vazio'],
      [/Math\.random\s*\(/g, 'Math.random'],
      [/href\s*=\s*["'](?:#|javascript:void[^"']*)["']/gi, 'link vazio'],
      ...(extname(file) === '.cshtml' ? [[/<button(?![^>]*\btype=)[^>]*>/gi, 'button sem type']] : []),
      [/\b(?:double|float)\s+(?:amount|price|cost|total|discount|margin|tax|retention)\b/gi, 'dinheiro não decimal'],
      [/Guid\.Parse\s*\(/g, 'Guid.Parse direto']
    ];
    for (const [pattern, label] of rules) for (const match of source.matchAll(pattern)) {
      const line = source.slice(0, match.index).split('\n').length;
      findings.push(`${rel}:${line} ${label}`);
    }
  }
}
for (const scope of scopes) walk(join(root, scope));
if (findings.length) { console.error(findings.join('\n')); process.exit(1); }
console.log('Sprint 49 refinement safety checks passed.');
