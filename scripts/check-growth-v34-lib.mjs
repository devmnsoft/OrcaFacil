import fs from 'node:fs';
import path from 'node:path';
const root = process.cwd();
export function requireFiles(label, files, tokens = []) {
  const missing = files.filter(file => !fs.existsSync(path.join(root, file)));
  const corpus = files.filter(file => fs.existsSync(path.join(root, file))).map(file => fs.readFileSync(path.join(root, file), 'utf8')).join('\n');
  const absent = tokens.filter(token => !corpus.includes(token));
  if (missing.length || absent.length) {
    console.error(`[${label}] falhou`, { missing, absent }); process.exit(1);
  }
  console.log(`[${label}] OK (${files.length} contrato(s), ${tokens.length} regra(s))`);
}
