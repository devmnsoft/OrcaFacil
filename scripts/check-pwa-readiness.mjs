import { complete, exists, read, requireCheck } from './sprint17-check-utils.mjs';
const manifest = JSON.parse(read('src/OrcaFacil.Web/wwwroot/site.webmanifest'));
for (const key of ['name','short_name','description','start_url','scope','display','background_color','theme_color','icons','categories','lang']) requireCheck(manifest[key], `Manifest sem ${key}.`);
for (const icon of manifest.icons) requireCheck(exists(`src/OrcaFacil.Web/wwwroot/${icon.src.replace(/^\//, '')}`), `Ícone inexistente: ${icon.src}`);
requireCheck(read('src/OrcaFacil.Web/Pages/Shared/_Layout.cshtml').includes('rel="manifest"'), 'Layout autenticado sem manifest.');
requireCheck(exists('src/OrcaFacil.Web/Pages/Offline.cshtml'), 'Página offline ausente.');
complete('PWA pronta para instalação');
