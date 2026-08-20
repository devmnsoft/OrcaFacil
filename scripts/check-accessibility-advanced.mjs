import { complete, read, requireCheck } from './sprint17-check-utils.mjs';
for (const file of ['src/OrcaFacil.Web/Pages/Shared/_Layout.cshtml','src/OrcaFacil.Web/Pages/Shared/_PublicLayout.cshtml']) {
  const html = read(file);
  requireCheck(html.includes('of-skip-link') && html.includes('id="main-content"'), `${file}: skip link ausente.`);
}
requireCheck(read('src/OrcaFacil.Web/wwwroot/js/offline-status.js').includes('aria-live'), 'Estado offline sem aria-live.');
complete('acessibilidade estrutural');
