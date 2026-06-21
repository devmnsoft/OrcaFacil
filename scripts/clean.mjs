import fs from 'node:fs/promises';
import path from 'node:path';
const dist = path.resolve('dist');
await fs.rm(dist, { recursive: true, force: true });
await fs.mkdir(dist, { recursive: true });
console.log('[clean] dist limpa');
