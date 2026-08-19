import { pagesRoot, sourcesUnder, finish } from './ui-check-helpers.mjs';

const pages = await sourcesUnder(pagesRoot, '.cshtml');
const failures = [];
for (const { file, source } of pages) {
  if (/<a\b[^>]*href\s*=\s*["'](?:|#|javascript:void[^"']*)["']/i.test(source)) failures.push(`${file}: link sem destino real`);
  if (/<img\b(?![^>]*\balt\s*=)[^>]*>/i.test(source)) failures.push(`${file}: imagem sem alt`);
  if (/<table\b/i.test(source) && !/<thead\b/i.test(source)) failures.push(`${file}: tabela sem thead`);
  if (/<(?:dialog|div)\b[^>]*class=["'][^"']*(?:of-dialog|of-modal)[^"']*["'][^>]*>/i.test(source)
      && !/<button\b[^>]*(?:data-(?:dialog|modal|overlay)-close|aria-label=["'][^"']*fechar)/i.test(source)) failures.push(`${file}: diálogo sem controle de fechar identificável`);
  if (/class=["'][^"']*of-toast/i.test(source) && !/aria-live\s*=|role\s*=\s*["'](?:status|alert)/i.test(source)) failures.push(`${file}: toast sem região viva`);
}
finish('Acessibilidade básica', failures, pages.length);
