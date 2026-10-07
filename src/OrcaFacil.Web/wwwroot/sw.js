/* OrçaFácil V1.8: only immutable/public shell assets are stored offline. */
'use strict';
const CACHE_NAME = 'orcafacil-public-v1.8.1';
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
const SENSITIVE_PATH = /^\/(Admin|Api|Auth|Clients|Documents|Files|Notifications|Payments|PublicQuotes|Receipts|Receivables|Settings|WorkOrders)(\/|$)/i;

self.addEventListener('install', event => {
  event.waitUntil(caches.open(CACHE_NAME).then(cache => cache.addAll(PUBLIC_ASSETS)));
  self.skipWaiting();
});

self.addEventListener('activate', event => {
  event.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key)))));
  self.clients.claim();
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
  event.respondWith(caches.match(url.pathname).then(cached => cached || fetch(request)));
});
