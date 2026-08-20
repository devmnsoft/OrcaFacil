import { complete, read, requireCheck } from './sprint17-check-utils.mjs';
const sw = read('src/OrcaFacil.Web/wwwroot/sw.js');
requireCheck(sw.includes("request.method !== 'GET'"), 'Service worker deve ignorar gravações.');
requireCheck(sw.includes('SENSITIVE_PATH') && sw.includes('PublicQuotes') && sw.includes('Payments') && sw.includes('Admin'), 'Rotas sensíveis não estão excluídas.');
requireCheck(!/cache\.put\s*\(\s*request/i.test(sw), 'Cache dinâmico de respostas detectado.');
complete('service worker conservador');
