import fs from 'node:fs/promises';
import path from 'node:path';

const root = process.cwd();
const src = path.join(root, 'public');
const dist = path.join(root, 'dist');
const skipDirs = new Set(['js']);
const skipFiles = new Set(['README.md', 'ARCHITECTURE.md']);

async function copyDir(from, to) {
  await fs.mkdir(to, { recursive: true });
  for (const entry of await fs.readdir(from, { withFileTypes: true })) {
    if (entry.name.startsWith('.') || skipFiles.has(entry.name)) continue;
    const s = path.join(from, entry.name);
    const d = path.join(to, entry.name);
    if (entry.isDirectory()) {
      if (skipDirs.has(entry.name)) continue;
      await copyDir(s, d);
    } else if (!entry.name.endsWith('.map')) {
      await fs.copyFile(s, d);
    }
  }
}

await copyDir(src, dist);
console.log('[copy-static] arquivos estáticos copiados sem public/js original');
