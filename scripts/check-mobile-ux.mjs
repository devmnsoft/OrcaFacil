import { complete, read, requireCheck } from './sprint17-check-utils.mjs';
const layout = read('src/OrcaFacil.Web/Pages/Shared/_Layout.cshtml');
const app = read('src/OrcaFacil.Web/wwwroot/js/app.js');
requireCheck(layout.includes('of-skip-link') && layout.includes('data-menu-toggle'), 'Shell mobile incompleto.');
requireCheck(app.includes("e.key==='Escape'") && app.includes("addEventListener('pointerdown'"), 'Menu não fecha por teclado/toque externo.');
complete('shell mobile');
