import { readdir, stat } from 'node:fs/promises';
import { spawnSync } from 'node:child_process';
import path from 'node:path';
const roots = ['server.js', 'public/js', 'tests'];
async function files(entry) {
  const s = await stat(entry);
  if (s.isFile()) return /\.(m?js)$/.test(entry) ? [entry] : [];
  const out = [];
  for (const name of await readdir(entry)) out.push(...await files(path.join(entry, name)));
  return out;
}
const list = (await Promise.all(roots.map(files))).flat();
let failed = false;
for (const file of list) {
  const r = spawnSync(process.execPath, ['--check', file], { stdio: 'inherit' });
  if (r.status !== 0) failed = true;
}
if (failed) process.exit(1);
console.log(`JS check OK (${list.length} arquivos).`);
