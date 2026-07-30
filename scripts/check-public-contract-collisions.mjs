import { readFile, readdir } from 'node:fs/promises';
import { join } from 'node:path';

const roots = ['src/OrcaFacil.Application'];
const guarded = new Set([
  'RevisionResult', 'PublicQuoteResult', 'PublicDecisionResult', 'QuoteLifecycleResult',
  'WorkOrderResult', 'PaymentRegistrationResult', 'ReceiptGenerationResult'
]);
const declarations = new Map();

async function visit(directory) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) await visit(path);
    else if (entry.name.endsWith('.cs')) {
      const source = await readFile(path, 'utf8');
      const namespace = source.match(/namespace\s+([\w.]+)\s*[;{]/)?.[1] ?? '<global>';
      for (const match of source.matchAll(/public\s+(?:(?:sealed|abstract|partial|static)\s+)*(?:record(?:\s+class|\s+struct)?|class|struct|interface|enum)\s+(\w+)/g)) {
        if (!guarded.has(match[1])) continue;
        const locations = declarations.get(match[1]) ?? [];
        locations.push(`${namespace} (${path})`);
        declarations.set(match[1], locations);
      }
    }
  }
}

for (const root of roots) await visit(root);
const collisions = [...declarations].filter(([, locations]) => locations.length > 1);
if (collisions.length) {
  console.error('Public application contract collisions detected:');
  for (const [name, locations] of collisions) console.error(`- ${name}: ${locations.join(', ')}`);
  process.exit(1);
}
console.log(`Public contract collision check passed (${declarations.size} guarded contracts found).`);
