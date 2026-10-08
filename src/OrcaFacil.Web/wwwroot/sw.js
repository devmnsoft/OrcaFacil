/* OrçaFácil: cache público versionado. Páginas autenticadas e respostas privadas ficam fora. */
'use strict';
const CACHE_PREFIX = 'orcafacil-public-';
const CACHE_NAME = 'orcafacil-public-v1.8.2';
const PUBLIC_ASSETS = [
  '/Offline',
  '/favicon.svg',
  '/img/brand/orcafacil-symbol.svg',
  '/css/app.css',
  '/css/tokens.css',
  '/css/base.css',
  '/css/surfaces.css',
  '/css/components.css',
  '/css/forms.css',
  '/css/design-system.css',
  '/css/navigation.css',
  '/css/tables.css',
  '/css/overlays.css',
  '/css/feedback.css',
  '/css/timeline.css',
  '/css/kanban.css',
  '/css/pipeline.css',
  '/css/public.css',
  '/css/auth.css',
  '/css/app-layout.css',
  '/css/admin.css',
  '/css/legal.css',
  '/css/responsive.css',
  '/css/mobile.css',
  '/css/settings.css',
  '/css/support.css',
  '/js/pwa-install.js',
  '/js/offline-status.js'
];
const SENSITIVE_PATH = /^\/(Admin|Api|Auth|Clients|Documents|Files|Notifications|Payments|PublicQuotes|Receipts|Receivables|Settings|WorkOrders|Subscription|Services|Profile)(\/|$)/i;

self.addEventListener('install', event => {
  event.waitUntil((async () => {
    const cache = await caches.open(CACHE_NAME);
    await Promise.all(PUBLIC_ASSETS.map(async path => {
      try {
        const response = await fetch(new Request(path, { cache: 'reload' }));
        if (response.ok && response.type === 'basic') await cache.put(path, response);
      } catch {
        /* Um asset ausente não impede o registro desta versão. */
      }
    }));
  })());
  self.skipWaiting();
});

self.addEventListener('activate', event => {
  event.waitUntil((async () => {
    const keys = await caches.keys();
    await Promise.all(keys
      .filter(key => key.startsWith(CACHE_PREFIX) && key !== CACHE_NAME)
      .map(key => caches.delete(key)));
  })());
});

self.addEventListener('fetch', event => {
  const request = event.request;
  if (request.method !== 'GET') return;
  const url = new URL(request.url);
  if (url.origin !== self.location.origin || SENSITIVE_PATH.test(url.pathname)) return;

  if (request.mode === 'navigate') {
    event.respondWith(fetch(request).catch(() => caches.match('/Offline')));
    return;
  }

  if (!PUBLIC_ASSETS.includes(url.pathname)) return;
  event.respondWith((async () => {
    const cache = await caches.open(CACHE_NAME);
    const cached = await cache.match(url.pathname);
    if (cached) return cached;
    const response = await fetch(request);
    if (response.ok && response.type === 'basic') await cache.put(url.pathname, response.clone());
    return response;
  })());
});
