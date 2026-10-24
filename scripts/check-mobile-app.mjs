import fs from 'node:fs';

// Valida a experiência de app móvel sobre o PWA existente: shortcuts de lançamento rápido no
// manifest, uma bottom tab bar de campo autenticada que só aparece no modo standalone e em tela
// pequena, ajuste de safe-area (notch) e inset do conteúdo — sem tocar na faixa de segurança do
// service worker (rotas autenticadas continuam fora do cache) nem nos tokens que o check:pwa exige.

const www = 'src/OrcaFacil.Web/wwwroot';
const read = (p) => fs.readFileSync(p, 'utf8');
const exists = (p) => fs.existsSync(p);
const failures = [];
const fail = (m) => failures.push(m);

// ---------- 1. Manifest: shortcuts de lançamento rápido ----------
let manifest = {};
try { manifest = JSON.parse(read(`${www}/site.webmanifest`)); } catch (e) { fail(`manifest não é JSON válido: ${e.message}`); }
const shortcuts = Array.isArray(manifest.shortcuts) ? manifest.shortcuts : [];
if (shortcuts.length < 4) fail('manifest sem shortcuts de lançamento rápido (>= 4)');
const EXPECTED_ROUTES = ['/Dashboard', '/Schedule', '/WorkOrders', '/Payments', '/Fiscal'];
const got = shortcuts.map((s) => s.url);
for (const r of EXPECTED_ROUTES) if (!got.includes(r)) fail(`shortcuts sem a rota principal: ${r}`);
for (const s of shortcuts) {
  if (!s.name || !String(s.name).trim()) fail(`shortcut sem nome: ${s.url}`);
  if (!s.url || !s.url.startsWith('/')) fail(`shortcut com url inválida: ${s.url}`);
  else if (!EXPECTED_ROUTES.includes(s.url)) fail(`shortcut aponta para rota fora do app: ${s.url}`);
  for (const ic of s.icons || []) if (!exists(`${www}${ic.src}`)) fail(`ícone do shortcut não existe: ${ic.src}`);
}

// ---------- 2. Bottom tab bar de campo, autenticada, com os 5 atalhos ----------
const layout = read('src/OrcaFacil.Web/Pages/Shared/_Layout.cshtml');
if (!layout.includes('<nav class="of-app-tabbar"')) fail('layout sem a bottom tab bar (.of-app-tabbar)');
for (const r of EXPECTED_ROUTES)
  if (!layout.includes(`href="${r}" data-path-prefix="${r}"`)) fail(`tab bar sem o atalho de campo para ${r}`);
const navIdx = layout.indexOf('<nav class="of-app-tabbar"');
const guardIdx = navIdx === -1 ? -1 : layout.lastIndexOf('@if (User.Identity?.IsAuthenticated == true)', navIdx);
if (guardIdx === -1 || navIdx - guardIdx > 200) fail('bottom tab bar não está dentro da guarda de autenticação');

// ---------- 3. CSS: oculta por padrão, revelada só em standalone, safe-area + inset ----------
const css = read(`${www}/css/mobile-app.css`);
if (!/\.of-app-tabbar\s*{[^}]*display:\s*none/.test(css)) fail('css: tab bar não fica oculta por padrão');
if (!css.includes('body.of-appstandalone .of-app-tabbar { display: flex; }')) fail('css: tab bar não é revelada apenas no modo standalone');
if (!css.includes('env(safe-area-inset-bottom)')) fail('css: sem ajuste de safe-area (notch) na barra inferior');
if (!css.includes('body.of-appstandalone .of-main')) fail('css: conteúdo não recebe inset para não ficar atrás da barra');

// ---------- 4. JS: ativa só em standalone + tela pequena e destaca o tab atual ----------
const js = read(`${www}/js/mobile-app-shell.js`);
if (!js.includes('(display-mode: standalone)') || !js.includes('navigator.standalone')) fail('js: sem detecção de modo standalone (incluindo iOS)');
if (!/window\.innerWidth\s*<=\s*(MAX_WIDTH|\d+)/.test(js) || !/MAX_WIDTH\s*=/.test(js)) fail('js: sem limite de largura para ativar a shell');
if (!js.includes("SHELL_CLASS = 'of-appstandalone'") || !js.includes('classList.toggle(SHELL_CLASS')) fail('js: não marca o <body> com a classe da shell');
if (!js.includes("addEventListener('resize'") || !js.includes('orientationchange')) fail('js: não re-avalia em resize/mudança de orientação');
if (!js.includes('data-path-prefix') || !js.includes('aria-current')) fail('js: não destaca o tab ativo pela rota');

// ---------- 5. Wiring no layout + tokens PWA intactos (sem regredir check:pwa) ----------
if (!layout.includes('css/mobile-app.css') || !layout.includes('js/mobile-app-shell.js')) fail('layout sem o link/css ou o script da shell móvel');
for (const token of ['rel="manifest"', 'apple-touch-icon', 'theme-color', 'pwa.js', 'pwa-install.js', 'offline-status.js'])
  if (!layout.includes(token)) fail(`layout: token PWA ausente (regrediu check:pwa): ${token}`);

if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log('App móvel validado: shortcuts de lançamento rápido no manifest, bottom tab bar de campo (autenticada, só em modo standalone e tela pequena), safe-area para notch e inset do conteúdo, com os tokens PWA intactos.');
