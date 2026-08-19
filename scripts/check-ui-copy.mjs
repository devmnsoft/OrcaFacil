import { pagesRoot, sourcesUnder, finish } from './ui-check-helpers.mjs';

const pages = await sourcesUnder(pagesRoot, '.cshtml');
const failures = [];
const forbidden = [
  ['texto provisório', /\b(?:lorem ipsum|em breve)\b/i],
  ['erro técnico genérico', />\s*Erro ao processar (?:a )?requisi(?:ção|cao)\.?\s*</i],
  ['ação genérica', />\s*Ação executada\.?\s*</i]
];
for (const { file, source } of pages) {
  for (const [label, pattern] of forbidden) if (pattern.test(source)) failures.push(`${file}: ${label}`);
  if (/<!--\s*TODO\b/i.test(source)) failures.push(`${file}: TODO visível na página`);
}
finish('Microcopy', failures, pages.length);
