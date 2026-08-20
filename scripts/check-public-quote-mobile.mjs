import { complete, read, requireCheck } from './sprint17-check-utils.mjs';
const page = read('src/OrcaFacil.Web/Pages/PublicQuotes/View.cshtml');
requireCheck(!/\b(Cost|Margin|Custo|Margem)\b/.test(page), 'Proposta pública expõe custo ou margem.');
requireCheck(/method="post"/i.test(page), 'Proposta pública sem ação POST.');
requireCheck(read('src/OrcaFacil.Web/wwwroot/css/public-quote.css').includes('@media'), 'Proposta sem regra responsiva.');
complete('proposta pública mobile');
