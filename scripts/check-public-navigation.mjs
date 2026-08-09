import { access, readFile } from 'node:fs/promises';
import { join } from 'node:path';

const pagesRoot = 'src/OrcaFacil.Web/Pages';
const publicFiles = [
  'Shared/_PublicLayout.cshtml',
  'Index.cshtml',
  'Precos.cshtml',
  'Support/Index.cshtml',
  'Auth/Login.cshtml',
  'Auth/Register.cshtml'
];

const failures = [];
for (const relativePath of publicFiles) {
  const source = await readFile(join(pagesRoot, relativePath), 'utf8');
  for (const match of source.matchAll(/<a\b[^>]*\basp-page="([^"]+)"[^>]*>/g)) {
    const page = match[1];
    const target = join(pagesRoot, `${page.replace(/^\//, '')}.cshtml`);
    try { await access(target); }
    catch { failures.push(`${relativePath}: asp-page="${page}" não existe (${target})`); }
  }
  if (/href="\/(?:#|(?!(?:\/)))/.test(source)) {
    failures.push(`${relativePath}: contém href absoluto dependente da raiz da aplicação`);
  }
}

const layout = await readFile(join(pagesRoot, 'Shared/_PublicLayout.cshtml'), 'utf8');
const home = await readFile(join(pagesRoot, 'Index.cshtml'), 'utf8');
for (const fragment of ['como-funciona', 'recursos', 'para-quem']) {
  if (!layout.includes(`asp-page="/Index" asp-fragment="${fragment}"`))
    failures.push(`menu público não usa Tag Helper para #${fragment}`);
  if (!home.includes(`id="${fragment}"`)) failures.push(`seção #${fragment} não existe na Home`);
}

if (failures.length) {
  console.error(`Falha na navegação pública:\n- ${failures.join('\n- ')}`);
  process.exit(1);
}
console.log(`Navegação pública validada em ${publicFiles.length} páginas; destinos e âncoras existem.`);
