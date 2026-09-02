import fs from 'node:fs';

export function read(path) { return fs.readFileSync(path, 'utf8'); }
export function requireAll(source, expectations, label) {
  const missing = expectations.filter(value => !source.includes(value));
  if (missing.length) throw new Error(`${label}: ausente(s): ${missing.join(', ')}`);
}
export function forbid(source, patterns, label) {
  const found = patterns.filter(pattern => pattern.test(source));
  if (found.length) throw new Error(`${label}: padrão proibido encontrado: ${found.join(', ')}`);
}
export function ok(label) { console.log(`OK: ${label}`); }
