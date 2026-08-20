(function () {
  'use strict';
  const DISMISSED_KEY = 'orcafacil.pwa-install-dismissed';
  if (window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true) return;

  window.addEventListener('beforeinstallprompt', event => {
    event.preventDefault();
    if (localStorage.getItem(DISMISSED_KEY) === 'true' || document.querySelector('[data-pwa-install]')) return;
    const region = document.createElement('aside');
    region.className = 'of-pwa-install';
    region.dataset.pwaInstall = '';
    region.setAttribute('aria-label', 'Instalação do OrçaFácil');
    region.innerHTML = '<span>Acesse o OrçaFácil mais rapidamente neste dispositivo.</span><div><button type="button" class="of-button of-button-primary" data-pwa-confirm>Instalar OrçaFácil</button><button type="button" class="of-button of-button-secondary" data-pwa-dismiss>Agora não</button></div>';
    document.body.appendChild(region);
    region.querySelector('[data-pwa-confirm]').addEventListener('click', async () => {
      region.remove();
      await event.prompt();
    });
    region.querySelector('[data-pwa-dismiss]').addEventListener('click', () => {
      localStorage.setItem(DISMISSED_KEY, 'true');
      region.remove();
    });
  }, { once: true });

  window.addEventListener('appinstalled', () => document.querySelector('[data-pwa-install]')?.remove());
}());
