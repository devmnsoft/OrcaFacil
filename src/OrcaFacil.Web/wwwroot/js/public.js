(function () {
  'use strict';
  const safeInit = (name, fn) => { try { fn(); } catch (error) { console.error(`[OrcaFácil:${name}]`, error); } };

  function getSamePageAnchorTarget(link) {
    if (!link) return null;
    const rawHref = link.getAttribute('href');
    if (!rawHref || rawHref === '#' || /^(mailto:|tel:|https?:\/\/)/i.test(rawHref)) return null;
    try {
      const url = new URL(link.href, window.location.href);
      if (url.origin !== location.origin || url.pathname !== location.pathname || !url.hash || url.hash === '#') return null;
      const id = decodeURIComponent(url.hash.slice(1));
      return id ? document.getElementById(id) : null;
    } catch { return null; }
  }

  function initMenu() {
    const button = document.querySelector('[data-public-menu-button]');
    const menu = document.querySelector('[data-public-menu]');
    if (!button || !menu) return;
    const setOpen = (open, restoreFocus = false) => {
      button.setAttribute('aria-expanded', String(open));
      button.setAttribute('aria-label', open ? 'Fechar menu de navegação' : 'Abrir menu de navegação');
      menu.classList.toggle('is-open', open);
      document.body.classList.toggle('of-menu-open', open);
      if (!open && restoreFocus) button.focus();
    };
    button.addEventListener('click', () => setOpen(button.getAttribute('aria-expanded') !== 'true'));
    menu.addEventListener('click', event => { if (event.target.closest('a[href]')) setOpen(false); });
    document.addEventListener('keydown', event => { if (event.key === 'Escape' && button.getAttribute('aria-expanded') === 'true') setOpen(false, true); });
    document.addEventListener('pointerdown', event => { if (button.getAttribute('aria-expanded') === 'true' && !menu.contains(event.target) && !button.contains(event.target)) setOpen(false); });
  }

  function initAnchors() {
    document.querySelectorAll('a[href*="#"]').forEach(link => link.addEventListener('click', event => {
      const target = getSamePageAnchorTarget(link);
      if (!target) return;
      event.preventDefault();
      const hash = new URL(link.href, location.href).hash;
      target.scrollIntoView({ behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth', block: 'start' });
      history.pushState(null, '', hash);
    }));
  }

  function initForms() {
    document.querySelectorAll('[data-lead-form]').forEach(form => form.addEventListener('submit', event => {
      if (!form.checkValidity()) return;
      if (form.dataset.submitting === 'true') { event.preventDefault(); return; }
      form.dataset.submitting = 'true';
      const submit = event.submitter || form.querySelector('[data-submit-button]');
      if (submit) submit.disabled = true;
      form.querySelector('[data-submit-label]')?.setAttribute('hidden', '');
      form.querySelector('[data-submit-loading]')?.removeAttribute('hidden');
    }));
  }

  document.addEventListener('DOMContentLoaded', () => {
    safeInit('public-menu', initMenu); safeInit('anchors', initAnchors); safeInit('forms', initForms);
    safeInit('header', () => { const header = document.querySelector('[data-public-header]'); if (!header) return; const sync = () => header.classList.toggle('is-scrolled', scrollY > 12); addEventListener('scroll', sync, { passive: true }); sync(); });
  });
})();
