import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';

export const pagesRoot = 'src/OrcaFacil.Web/Pages';
export const cssRoot = 'src/OrcaFacil.Web/wwwroot/css';

export async function filesUnder(directory, extension) {
  const entries = await readdir(directory, { withFileTypes: true });
  return (await Promise.all(entries.map(entry => {
    const target = path.join(directory, entry.name);
    return entry.isDirectory() ? filesUnder(target, extension) : target.endsWith(extension) ? [target] : [];
  }))).flat();
}

export async function sourcesUnder(directory, extension) {
  const files = await filesUnder(directory, extension);
  return Promise.all(files.map(async file => ({ file, source: await readFile(file, 'utf8') })));
}

export function finish(name, failures, count) {
  if (failures.length) {
    console.error(`${name}: ${failures.length} problema(s) encontrado(s):\n${failures.join('\n')}`);
    process.exitCode = 1;
    return;
  }
  console.log(`${name}: ${count} arquivo(s) validados.`);
}
