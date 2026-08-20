(function () {
  'use strict';
  const message = 'Sem conexão. Esta ação precisa de internet para ser salva com segurança.';
  const banner = document.createElement('div');
  banner.className = 'of-offline-status';
  banner.setAttribute('role', 'status');
  banner.setAttribute('aria-live', 'polite');
  banner.hidden = navigator.onLine;
  banner.textContent = 'Você está sem conexão. A leitura desta tela pode estar desatualizada.';
  document.body.appendChild(banner);
  document.querySelector('[data-offline-retry]')?.addEventListener('click', () => location.reload());

  const update = () => { banner.hidden = navigator.onLine; };
  window.addEventListener('online', update);
  window.addEventListener('offline', update);
  document.addEventListener('submit', event => {
    if (navigator.onLine || (event.target.method || 'get').toLowerCase() === 'get') return;
    event.preventDefault();
    banner.hidden = false;
    banner.textContent = message;
    banner.focus();
  });
}());
