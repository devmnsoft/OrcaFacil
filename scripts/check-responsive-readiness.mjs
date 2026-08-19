import { cssRoot, pagesRoot, sourcesUnder, finish } from './ui-check-helpers.mjs';

const [styles, pages] = await Promise.all([sourcesUnder(cssRoot, '.css'), sourcesUnder(pagesRoot, '.cshtml')]);
const failures = [];
const css = styles.map(item => item.source).join('\n');
if (!/@media\s*\([^)]*max-width/i.test(css)) failures.push('CSS: nenhum breakpoint mobile encontrado');
if (!/max-width\s*:\s*(?:100%|calc|min\()/i.test(css)) failures.push('CSS: nenhum limite fluido de largura encontrado');
if (!/100d?vh/i.test(css)) failures.push('CSS: overlays não usam limite relativo à viewport');
for (const { file, source } of pages) {
  if (/<table\b/i.test(source) && !/(of-(?:responsive-)?table|data-table|[\w-]+-table-wrap|of-preview-document|receipt-table)/i.test(source)) failures.push(`${file}: tabela sem contrato responsivo`);
}
finish('Prontidão responsiva', failures, styles.length + pages.length);
