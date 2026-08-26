import { access, readFile } from 'node:fs/promises';
import { join } from 'node:path';

const pagesRoot = 'src/OrcaFacil.Web/Pages';
const publicFiles = [
  'Shared/_PublicLayout.cshtml',
  'Index.cshtml',
  'Precos.cshtml',
  'Support/Index.cshtml',
  'Auth/Login.cshtml',
  'Auth/Register.cshtml',
  'Termos.cshtml',
  'Privacidade.cshtml'
  ,'Recursos/Index.cshtml','Segmentos/Index.cshtml','Blog/Index.cshtml','Trust/Index.cshtml','Status.cshtml'
];

const failures = [];
for (const relativePath of publicFiles) {
  const source = await readFile(join(pagesRoot, relativePath), 'utf8');
  for (const invalid of source.matchAll(/<a\b[^>]*\bhref\s*=\s*["'](?:|#|\/#(?:[^"']*))["'][^>]*>/gi))
    failures.push(`${relativePath}: link sem destino real: ${invalid[0]}`);
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
const footerSource = layout.match(/<footer[\s\S]*?<\/footer>/i)?.[0] ?? '';
for (const requiredPage of ['/Index', '/Recursos/Index', '/Segmentos/Index', '/Precos', '/Auth/Login', '/Auth/Register', '/Termos', '/Privacidade', '/Cookies', '/Trust/Index', '/Status'])
  if (!footerSource.includes(`asp-page="${requiredPage}"`)) failures.push(`footer não aponta para ${requiredPage}`);
if (!footerSource.includes('href="mailto:')) failures.push('footer não possui e-mail comercial acionável');

for (const requiredLabel of ['Criar meu primeiro orçamento', 'Ver planos', 'Ver Profissional', 'Ver Negócio', 'Falar com suporte'])
  if (!home.includes(requiredLabel)) failures.push(`CTA obrigatório ausente na Home: ${requiredLabel}`);

for (const requiredMenuPage of ['/Recursos/Index', '/Segmentos/Index', '/Blog/Index', '/Status', '/Contato'])
  if (!layout.includes(`asp-page="${requiredMenuPage}"`)) failures.push(`menu público não aponta para ${requiredMenuPage}`);

if (failures.length) {
  console.error(`Falha na navegação pública:\n- ${failures.join('\n- ')}`);
  process.exit(1);
}
console.log(`Navegação pública validada em ${publicFiles.length} páginas; destinos e âncoras existem.`);
