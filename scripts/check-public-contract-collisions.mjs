import { readdir, readFile } from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';

const root = path.resolve('src/OrcaFacil.Application');
const guarded = new Set([
  'RevisionResult', 'PublicQuoteResult', 'PublicDecisionResult', 'QuoteLifecycleResult',
  'WorkOrderResult', 'PaymentRegistrationResult', 'ReceiptGenerationResult'
]);

async function files(directory) {
  const result = [];
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    if (entry.name === 'bin' || entry.name === 'obj') continue;
    const absolute = path.join(directory, entry.name);
    if (entry.isDirectory()) result.push(...await files(absolute));
    else if (entry.name.endsWith('.cs')) result.push(absolute);
  }
  return result;
}

const declarations = [];
for (const file of await files(root)) {
  const source = await readFile(file, 'utf8');
  const namespace = source.match(/namespace\s+([\w.]+)\s*[;{]/)?.[1];
  if (!namespace) continue;
  const pattern = /\bpublic\s+(?:(?:sealed|abstract|static|readonly|partial)\s+)*(?:record(?:\s+(?:class|struct))?|class|struct|interface|enum)\s+(\w+)/g;
  for (const match of source.matchAll(pattern)) {
    if (guarded.has(match[1])) declarations.push({ name: match[1], namespace, file: path.relative(process.cwd(), file) });
  }
}

const collisions = [...Map.groupBy(declarations, item => item.name)]
  .filter(([, items]) => items.length !== 1);
if (collisions.length) {
  console.error('Public application contract collisions found:');
  for (const [name, items] of collisions) {
    console.error(`- ${name}: ${items.length} declarations`);
    for (const item of items) console.error(`  ${item.namespace} (${item.file})`);
  }
  process.exitCode = 1;
} else {
  console.log(`Public contract collision check passed (${declarations.length} canonical contracts).`);
}
