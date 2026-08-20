(function () {
  'use strict';
  if (!('serviceWorker' in navigator)) return;
  const isSecure = location.protocol === 'https:' || ['localhost', '127.0.0.1'].includes(location.hostname);
  if (!isSecure) return;
  window.addEventListener('load', () => navigator.serviceWorker.register('/sw.js', { scope: '/' })
    .catch(error => console.warn('[OrçaFácil:PWA] Service worker indisponível.', error)));
}());
