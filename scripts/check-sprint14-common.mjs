import { readFileSync, existsSync } from 'node:fs';
import { resolve } from 'node:path';
export function requireFiles(label, files, markers = []) {
  const missing = files.filter(file => !existsSync(resolve(file)));
  if (missing.length) { console.error(`❌ ${label}: arquivos ausentes: ${missing.join(', ')}`); process.exit(1); }
  const body = files.map(file => readFileSync(resolve(file), 'utf8')).join('\n');
  const absent = markers.filter(marker => !body.includes(marker));
  if (absent.length) { console.error(`❌ ${label}: contratos ausentes: ${absent.join(', ')}`); process.exit(1); }
  console.log(`✅ ${label}: ${files.length} arquivos e ${markers.length} contratos validados.`);
}
