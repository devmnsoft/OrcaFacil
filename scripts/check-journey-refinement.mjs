import { readFileSync, readdirSync, statSync } from 'node:fs';
import { extname, join, relative } from 'node:path';

const root = process.cwd();
const pages = join(root, 'src', 'OrcaFacil.Web', 'Pages');
const findings = [];
function walk(directory) {
  for (const entry of readdirSync(directory)) {
    const file = join(directory, entry);
    if (statSync(file).isDirectory()) { walk(file); continue; }
    if (!['.cs', '.cshtml'].includes(extname(file))) continue;
    const source = readFileSync(file, 'utf8');
    const name = relative(root, file).replaceAll('\\', '/');
    const rules = [
      [/href\s*=\s*["'](?:#(?=["'])|javascript:void)/gi, 'link sem destino real'],
      [/throw\s+new\s+NotImplementedException/g, 'fluxo não implementado'],
      [/Math\.random\s*\(/g, 'score aleatório'],
      ...(extname(file) === '.cshtml' ? [[/<button(?![^>]*\btype=)[^>]*>/gi, 'botão sem tipo']] : [])
    ];
    for (const [pattern, label] of rules) if (pattern.test(source)) findings.push(`${name}: ${label}`);
  }
}
walk(pages);
const required = [
  'src/OrcaFacil.Application/Quality/BusinessRefinementServices.cs',
  'src/OrcaFacil.Web/Pages/Admin/JourneyRefinement/Index.cshtml',
  'src/OrcaFacil.Web/Pages/Admin/JourneyRefinement/Index.cshtml.cs'
];
for (const file of required) { try { readFileSync(join(root, file)); } catch { findings.push(`${file}: contrato obrigatório ausente`); } }
if (findings.length) { console.error(findings.join('\n')); process.exit(1); }
console.log('Sprint 50 journey refinement checks passed.');
