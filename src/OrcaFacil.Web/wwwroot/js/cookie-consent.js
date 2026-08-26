(() => {
  'use strict';
  const key = 'orcafacil.cookie-consent.v1';
  if (localStorage.getItem(key)) return;
  const banner = document.createElement('section');
  banner.className = 'of-cookie-banner'; banner.setAttribute('role', 'dialog'); banner.setAttribute('aria-label', 'Preferências de cookies');
  banner.innerHTML = '<p><strong>Cookies necessários</strong> mantêm login, segurança e preferências. Não carregamos analytics ou marketing externos nesta página.</p><div><a href="/Cookies">Saiba mais</a><button type="button" class="of-button of-button-primary">Entendi</button></div>';
  banner.querySelector('button')?.addEventListener('click', () => { localStorage.setItem(key, JSON.stringify({ version: 1, necessary: true, savedAt: new Date().toISOString() })); banner.remove(); });
  document.body.appendChild(banner);
})();
