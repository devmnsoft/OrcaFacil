import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';

const roots = [
  'src/OrcaFacil.Web',
  'src/OrcaFacil.Application',
  'src/OrcaFacil.Domain',
  'src/OrcaFacil.Persistence',
  'src/OrcaFacil.Infrastructure',
  'src/OrcaFacil.Shared',
  'tests'
];
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

const intelligenceReportPath = 'src/OrcaFacil.Web/Services/IntelligenceReportService.cs';
const intelligenceReport = await readFile(intelligenceReportPath, 'utf8');
const stageOrderMatches = [...intelligenceReport.matchAll(/private\s+static\s+int\s+StageOrder\s*\(string\s+stage\)/g)];
if (stageOrderMatches.length !== 1) {
  failures.push(`${intelligenceReportPath}: StageOrder deve existir exatamente uma vez`);
} else {
  const stageOrderStart = stageOrderMatches[0].index;
  const stageOrderEnd = intelligenceReport.indexOf('\n    }', stageOrderStart);
  const stageOrder = intelligenceReport.slice(stageOrderStart, stageOrderEnd + 6);
  const expectedStages = [
    'Rascunho',
    'Pronto',
    'Enviado',
    'Visualizado',
    'Em negociação',
    'Aprovado',
    'Recusado',
    'Expirado',
    'Convertido em OS'
  ];
  const actualStages = [...stageOrder.matchAll(/^\s*"([^"]+)"[,]?\s*$/gm)].map(match => match[1]);

  if (JSON.stringify(actualStages) !== JSON.stringify(expectedStages)) {
    failures.push(`${intelligenceReportPath}: StageOrder não preserva o funil oficial`);
  }
  if (/\bis\s+var\b|&&/.test(stageOrder)) {
    failures.push(`${intelligenceReportPath}: StageOrder deve usar variável explícita, sem \`is var\` ou \`&&\``);
  }
  if (!/var\s+index\s*=\s*Array\.IndexOf\(stages,\s*stage\);/.test(stageOrder) ||
      !/return\s+index\s*>=\s*0\s*\?\s*index\s*:\s*99;/.test(stageOrder)) {
    failures.push(`${intelligenceReportPath}: StageOrder deve usar Array.IndexOf e fallback 99`);
  }
}

if (failures.length) {
  console.error(`Bloqueadores de build e navegação encontrados:\n${failures.join('\n')}`);
  process.exit(1);
}
console.log('Build blockers: nenhum padrão de risco encontrado.');
