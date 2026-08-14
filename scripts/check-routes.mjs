import { access, readFile } from 'node:fs/promises';
import path from 'node:path';

const razorRoot = 'src/OrcaFacil.Web/Pages';
const sources = [
  'Shared/_PublicLayout.cshtml',
  'Shared/_Layout.cshtml',
  'Shared/Partials/_AuthenticatedNavigation.cshtml',
  'Index.cshtml'
];
const failures = [];

async function pageExists(page) {
  const normalized = page.replace(/^\//, '');
  for (const candidate of [`${normalized}.cshtml`, `${normalized}/Index.cshtml`]) {
    try { await access(path.join(razorRoot, candidate)); return true; } catch { /* try next convention */ }
  }
  return false;
}

for (const relative of sources) {
  const source = await readFile(path.join(razorRoot, relative), 'utf8');
  for (const match of source.matchAll(/<a\b[^>]*\basp-page="([^"]+)"[^>]*>/gi)) {
    if (match[0].includes('asp-area="Admin"')) continue;
    if (!await pageExists(match[1])) failures.push(`${relative}: página não encontrada: ${match[1]}`);
  }
  for (const match of source.matchAll(/<button\b([^>]*)>/gi)) {
    if (!/\btype\s*=/.test(match[1])) failures.push(`${relative}: button sem type: ${match[0]}`);
  }
  if (/\bIndexModel\s*\./.test(source)) failures.push(`${relative}: membro estático de IndexModel usado no Razor`);
}

if (failures.length) {
  console.error(`Falha no contrato de rotas:\n- ${failures.join('\n- ')}`);
  process.exit(1);
}
console.log(`Rotas e controles Razor validados em ${sources.length} superfícies de navegação.`);
