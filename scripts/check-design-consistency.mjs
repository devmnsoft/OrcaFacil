import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';

const webRoot = 'src/OrcaFacil.Web';
const pagesRoot = path.join(webRoot, 'Pages');
const cssRoot = path.join(webRoot, 'wwwroot', 'css');

async function walk(directory, extension) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = await Promise.all(entries.map(entry => {
    const target = path.join(directory, entry.name);
    return entry.isDirectory() ? walk(target, extension) : (target.endsWith(extension) ? [target] : []);
  }));
  return files.flat();
}

const pageFiles = await walk(pagesRoot, '.cshtml');
const cssFiles = await walk(cssRoot, '.css');
const pageSources = await Promise.all(pageFiles.map(async file => [file, await readFile(file, 'utf8')]));
const cssSource = (await Promise.all(cssFiles.map(file => readFile(file, 'utf8')))).join('\n');
const markupSource = pageSources.map(([, source]) => source).join('\n');
const failures = [];

const requiredComponents = [
  'of-card', 'of-field', 'of-form-grid', 'of-button-primary', 'of-button-secondary',
  'of-status-badge', 'of-empty-state', 'of-table', 'of-drawer', 'of-toast'
];
for (const className of requiredComponents) {
  if (!cssSource.includes(`.${className}`)) failures.push(`${className}: sem definição CSS`);
  if (!markupSource.includes(className)) failures.push(`${className}: sem uso real em Razor`);
}

const legacyPatterns = [
  ['controle Bootstrap obrigatório', /\bdata-bs-(?:toggle|target|dismiss)\s*=/g],
  ['classe Bootstrap de botão', /\bclass\s*=\s*["'][^"']*(?:^|\s)btn(?:\s|$)/g]
];
for (const [file, source] of pageSources) {
  for (const [description, pattern] of legacyPatterns) {
    pattern.lastIndex = 0;
    if (pattern.test(source)) failures.push(`${file}: ${description}`);
  }
}

if (failures.length) {
  console.error(`Inconsistências do design system encontradas:\n${failures.join('\n')}`);
  process.exit(1);
}

console.log(`Design system validado em ${pageFiles.length} páginas e ${cssFiles.length} folhas de estilo.`);
