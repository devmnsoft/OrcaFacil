(function () {
  'use strict';
  if (!('serviceWorker' in navigator)) return;
  const isSecure = location.protocol === 'https:' || ['localhost', '127.0.0.1'].includes(location.hostname);
  if (!isSecure) return;

  const hadController = Boolean(navigator.serviceWorker.controller);
  const showUpdate = () => {
    if (document.querySelector('[data-pwa-update]')) return;
    const banner = document.createElement('div');
    banner.className = 'of-offline-status';
    banner.dataset.pwaUpdate = 'true';
    banner.setAttribute('role', 'status');
    const text = document.createElement('p');
    text.textContent = 'Há uma versão nova do OrçaFácil. Recarregue esta aba para usar os arquivos correspondentes. Operações que dependem do servidor continuam exigindo conexão.';
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'of-button';
    button.textContent = 'Recarregar';
    button.addEventListener('click', () => location.reload());
    banner.append(text, button);
    document.body.appendChild(banner);
  };

  window.addEventListener('load', () => navigator.serviceWorker.register('/sw.js', { scope: '/' })
    .then(registration => {
      registration.addEventListener('updatefound', () => {
        const worker = registration.installing;
        worker?.addEventListener('statechange', () => {
          if (worker.state === 'activated' && hadController) showUpdate();
        });
      });
    })
    .catch(error => console.warn('[OrçaFácil:PWA] Service worker indisponível.', error)));
}());
