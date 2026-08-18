import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';

const root = 'src/OrcaFacil.Web/Pages';
async function walk(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  return (await Promise.all(entries.map(entry => {
    const target = path.join(directory, entry.name);
    if (entry.isDirectory()) return ['bin', 'obj'].includes(entry.name) ? [] : walk(target);
    return [target];
  }))).flat();
}

const files = await walk(root);
const pageModels = files.filter(file => file.endsWith('.cshtml.cs'));
const staticMembers = new Map();
const instanceMembers = new Map();

for (const file of pageModels) {
  const source = await readFile(file, 'utf8');
  const relativePage = file.slice(0, -3);
  staticMembers.set(relativePage, new Set([...source.matchAll(/public\s+static\s+[\w<>,?\[\].]+\s+(\w+)\s*\(/g)].map(match => match[1])));
  instanceMembers.set(relativePage, new Set([...source.matchAll(/public\s+(?!static\b)[\w<>,?\[\].]+\s+(\w+)\s*\(/g)].map(match => match[1])));
}

const failures = [];
for (const file of files.filter(candidate => candidate.endsWith('.cshtml'))) {
  const source = await readFile(file, 'utf8');
  for (const member of staticMembers.get(file) ?? []) {
    if (new RegExp(`@?Model\\.${member}\\s*\\(`).test(source)) failures.push(`${file}: Model acessa o membro estático ${member}`);
  }
}

const testFiles = (await walk('tests')).filter(file => file.endsWith('.cs'));
const allInstanceMembers = new Set([...instanceMembers.values()].flatMap(set => [...set]));
for (const file of testFiles) {
  const lines = (await readFile(file, 'utf8')).split(/\r?\n/);
  lines.forEach((line, index) => {
    for (const match of line.matchAll(/\bIndexModel\.(\w+)\s*\(/g)) {
      if (allInstanceMembers.has(match[1])) failures.push(`${file}:${index + 1}: IndexModel.${match[1]} chama método de instância como estático`);
    }
  });
}

if (failures.length) {
  console.error(`Riscos static/instance em Razor Pages:\n${failures.join('\n')}`);
  process.exit(1);
}
console.log('Razor static/instance: chamadas validadas.');
