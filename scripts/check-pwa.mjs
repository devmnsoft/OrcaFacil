import fs from 'node:fs';

// Valida o contrato PWA do Fechô: manifest instalável (PNG 192/512 + SVG,
// apple-touch-icon p/ iOS), service worker com precache íntegro e faixa de
// segurança (rotas privadas fora do cache; navegação em rede com fallback
// /Offline; nenhum dado autenticado em cache), registro nos dois layouts e
// honestidade do banner de atualização (recarrega só quando o usuário pede).

const www = 'src/OrcaFacil.Web/wwwroot';
const fail = (msg) => { throw new Error(`check-pwa: ${msg}`); };
const read = (p) => fs.readFileSync(p, 'utf8');
const exists = (p) => fs.existsSync(p);

// ---------- 1. Manifest instalável ----------
const manifestPath = `${www}/site.webmanifest`;
const manifest = JSON.parse(read(manifestPath));
for (const key of ['name', 'short_name', 'start_url', 'scope', 'display', 'theme_color', 'lang'])
  if (!manifest[key]) fail(`manifest sem campo obrigatório: ${key}`);
if (manifest.display !== 'standalone') fail('manifest.display precisa ser standalone');

const iconSrcs = (manifest.icons || []).map((i) => i.src);
for (const src of iconSrcs) if (!exists(`${www}${src}`)) fail(`ícone do manifest não existe: ${src}`);

const hasSvg = (manifest.icons || []).some((i) => i.type === 'image/svg+xml');
const png192 = (manifest.icons || []).find((i) => i.sizes === '192x192' && i.type === 'image/png');
const png512 = (manifest.icons || []).find((i) => i.sizes === '512x512' && i.type === 'image/png');
if (!hasSvg) fail('manifest sem ícone SVG');
if (!png192) fail('manifest sem ícone PNG 192x192 (critério de instalabilidade do Android)');
if (!png512) fail('manifest sem ícone PNG 512x512 (splash/inicialização)');

// Dimensões reais dos PNGs (leitura de IHDR, não confiar só na declaração)
function pngSize(file) {
  const b = fs.readFileSync(file);
  const sig = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
  if (!sig.every((v, i) => b[i] === v)) fail(`${file}: não é PNG válido`);
  return { w: b.readUInt32BE(16), h: b.readUInt32BE(20) };
}
for (const [file, expected] of [
  [`${www}${png192.src}`, 192],
  [`${www}${png512.src}`, 512],
]) {
  const { w, h } = pngSize(file);
  if (w !== expected || h !== expected) fail(`${file}: esperado ${expected}x${expected}, obtido ${w}x${h}`);
}

// ---------- 2. iOS: apple-touch-icon nos dois layouts ----------
const appleIcon = `${www}/img/brand/apple-touch-icon.png`;
if (!exists(appleIcon)) fail('apple-touch-icon.png ausente (iOS usaria screenshot da página)');
const apple = pngSize(appleIcon);
if (apple.w !== 180 || apple.h !== 180) fail(`apple-touch-icon precisa ser 180x180, obtido ${apple.w}x${apple.h}`);

for (const layout of ['src/OrcaFacil.Web/Pages/Shared/_Layout.cshtml', 'src/OrcaFacil.Web/Pages/Shared/_PublicLayout.cshtml']) {
  const text = read(layout);
  for (const token of ['rel="manifest"', 'apple-touch-icon', 'theme-color', 'pwa.js', 'pwa-install.js', 'offline-status.js'])
    if (!text.includes(token)) fail(`${layout}: sem ${token}`);
}

// ---------- 3. Service worker: precache íntegro e seguro ----------
const sw = read(`${www}/sw.js`);
const assetsBlock = /const PUBLIC_ASSETS = \[([\s\S]*?)\];/.exec(sw);
if (!assetsBlock) fail('sw.js: lista PUBLIC_ASSETS não encontrada');
const assets = [...assetsBlock[1].matchAll(/'([^']+)'/g)].map((m) => m[1]);
if (assets.length < 10) fail('sw.js: precache curto demais');
for (const path of assets) {
  if (path === '/Offline') {
    const offline = read('src/OrcaFacil.Web/Pages/Offline.cshtml');
    if (!offline.includes('@page "/Offline"')) fail('página /Offline sem rota');
    if (offline.includes('[Authorize]')) fail('/Offline precisa ser anônima (é o fallback offline)');
    continue;
  }
  if (!exists(`${www}${path}`)) fail(`sw.js precacha asset inexistente: ${path}`);
}

// Faixa de segurança: rotas privadas fora do cache (sessão/dados de conta)
const sensitive = /const SENSITIVE_PATH = (.+);/.exec(sw);
if (!sensitive) fail('sw.js: SENSITIVE_PATH ausente');
for (const route of ['Admin', 'Api', 'Auth', 'Settings', 'Clients', 'Payments', 'Subscription'])
  if (!sensitive[1].includes(route)) fail(`SENSITIVE_PATH não exclui ${route}`);

// Navegação: rede primeiro com fallback /Offline; nada de cache de navegação
if (!sw.includes("fetch(request).catch(() => caches.match('/Offline'))")) fail('sw.js: fallback de navegação offline ausente');
if (/navigate[\s\S]{0,400}cache\.put\(request/.test(sw)) fail('sw.js: navegacao nao pode ser cacheada (conteria dados de sessao)');

// ---------- 4. Registro e honestidade ----------
const pwaJs = read(`${www}/js/pwa.js`);
if (!pwaJs.includes("navigator.serviceWorker.register('/sw.js', { scope: '/' })")) fail('pwa.js: registro do service worker ausente');
if (!pwaJs.includes('https:') || !pwaJs.includes('localhost')) fail('pwa.js: gate de contexto seguro ausente');
if (!pwaJs.includes('Recarregar')) fail('pwa.js: atualização precisa pedir confirmação (banner), não recarregar sozinha');

const offlineJs = read(`${www}/js/offline-status.js`);
if (!offlineJs.includes('preventDefault')) fail('offline-status.js: deve bloquear envio de formulário offline (não simular sucesso)');

console.log(`PWA: manifest instalável (SVG + PNG 192/512 + apple-touch-icon 180), ${assets.length} assets em precache, rotas privadas fora do cache, fallback /Offline e banner de atualização honesto OK.`);
