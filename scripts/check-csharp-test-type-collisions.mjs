import { readdir, readFile } from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';

const root = path.resolve('tests/OrcaFacil.UnitTests');
const generatedFile = /(?:\.g(?:enerated)?|\.designer|\.assemblyattributes)\.cs$/i;
// Add a fully-qualified type here only when its split is intentional and reviewed.
const authorizedPartialTypes = new Set([]);

async function csFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = [];
  for (const entry of entries) {
    if (entry.isDirectory() && (entry.name === 'bin' || entry.name === 'obj')) continue;
    const absolute = path.join(directory, entry.name);
    if (entry.isDirectory()) files.push(...await csFiles(absolute));
    else if (entry.name.endsWith('.cs') && !generatedFile.test(entry.name)) files.push(absolute);
  }
  return files;
}

function withoutCommentsAndStrings(source) {
  return source
    .replace(/\/\*[\s\S]*?\*\//g, match => match.replace(/[^\n]/g, ' '))
    .replace(/\/\/[^\n]*/g, '')
    .replace(/@?"(?:""|\\.|[^"\n])*"/g, match => match.replace(/[^\n]/g, ' '))
    .replace(/'(?:\\.|[^'\n])'/g, ' ');
}

function declarations(source, file) {
  const clean = withoutCommentsAndStrings(source);
  const lines = clean.split(/\r?\n/);
  const found = [];
  let depth = 0;
  let fileNamespace = '';
  const namespaceBlocks = [];

  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    const namespace = line.match(/^\s*namespace\s+([\w.]+)\s*([;{])/);
    if (namespace) {
      if (namespace[2] === ';') fileNamespace = namespace[1];
      else namespaceBlocks.push({ name: namespace[1], depth: depth + 1 });
    }

    const activeNamespace = namespaceBlocks.at(-1)?.name ?? fileNamespace;
    const topLevelDepth = namespaceBlocks.length ? namespaceBlocks.at(-1).depth : 0;
    if (depth === topLevelDepth) {
      const type = line.match(/^\s*(?:(?:public|internal|private|protected|sealed|abstract|static|readonly|ref|file|new|unsafe)\s+)*(partial\s+)?(class|record|struct|enum|interface)\s+(?:(?:class|struct)\s+)?([A-Za-z_]\w*)/);
      if (type) found.push({ namespace: activeNamespace, name: type[3], kind: type[2], partial: Boolean(type[1]), file, line: index + 1 });
    }

    depth += [...line].filter(character => character === '{').length;
    depth -= [...line].filter(character => character === '}').length;
    while (namespaceBlocks.length && depth < namespaceBlocks.at(-1).depth) namespaceBlocks.pop();
  }
  return found;
}

const all = [];
for (const file of await csFiles(root)) {
  const relative = path.relative(process.cwd(), file).replaceAll(path.sep, '/');
  all.push(...declarations(await readFile(file, 'utf8'), relative));
}

const grouped = new Map();
for (const item of all) {
  const key = `${item.namespace}.${item.name}`;
  grouped.set(key, [...(grouped.get(key) ?? []), item]);
}
const collisions = [...grouped]
  .filter(([fullName, items]) => items.length > 1 &&
    !(authorizedPartialTypes.has(fullName) && items.every(item => item.partial)));

if (collisions.length) {
  console.error('C# test type collisions found:');
  for (const [fullName, items] of collisions) {
    console.error(`\n- ${fullName}`);
    for (const item of items) console.error(`  ${item.file}:${item.line} (${item.kind}${item.partial ? ', partial' : ''})`);
  }
  process.exitCode = 1;
} else {
  console.log(`C# test type collision check passed (${all.length} top-level types in ${new Set(all.map(item => item.file)).size} files).`);
}
