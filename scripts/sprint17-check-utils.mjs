import fs from 'node:fs';
import path from 'node:path';

export const root = path.resolve(import.meta.dirname, '..');
export const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
export const exists = relative => fs.existsSync(path.join(root, relative));
export function requireCheck(condition, message) {
  if (!condition) throw new Error(message);
}
export function complete(name) { console.log(`OK: ${name}`); }
