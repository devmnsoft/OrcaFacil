import fs from 'node:fs';
import path from 'node:path';

export function checkField(feature, required = []) {
  const roots = ['src/OrcaFacil.Web', 'src/OrcaFacil.Application', 'src/OrcaFacil.Domain', 'src/OrcaFacil.Persistence', 'tests/OrcaFacil.UnitTests'];
  const files = roots.flatMap(walk).filter(file => /\.(cs|cshtml|js)$/.test(file));
  const source = files.map(file => `${file}\n${fs.readFileSync(file, 'utf8')}`).join('\n');
  const failures = [];
  for (const token of required) if (!source.includes(token)) failures.push(`recurso ausente: ${token}`);
  const forbidden = [
    [/Math\.random\s*\(/, 'Math.random em operação/relatório'],
    [/javascript\s*:\s*void/i, 'javascript:void'],
    [/href\s*=\s*["']#["']/i, 'link vazio'],
    [/NotImplementedException/, 'NotImplementedException'],
    [/(?:signature|assinatura)[^\n]{0,50}(?:certified|certificada)[^\n]{0,20}(?:true|sim)/i, 'certificação de assinatura sem provedor']
  ];
  for (const [pattern, label] of forbidden) if (pattern.test(source)) failures.push(label);
  if (failures.length) { console.error(`Field ${feature}: FALHOU\n- ${[...new Set(failures)].join('\n- ')}`); process.exit(1); }
  console.log(`Field ${feature}: OK (${files.length} arquivos ASP.NET verificados).`);
}

function walk(root) {
  if (!fs.existsSync(root)) return [];
  return fs.readdirSync(root, { withFileTypes: true }).flatMap(entry => {
    const item = path.join(root, entry.name);
    if (entry.isDirectory()) return ['bin', 'obj'].includes(entry.name) ? [] : walk(item);
    return [item];
  });
}
