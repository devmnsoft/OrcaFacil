const button = document.querySelector('[data-public-menu-button]');
const menu = document.querySelector('[data-public-menu]');
if (button && menu) {
  const isOpen = () => button.getAttribute('aria-expanded') === 'true';
  const close = (restoreFocus = false) => {
    if (!isOpen()) return;
    button.setAttribute('aria-expanded', 'false');
    button.setAttribute('aria-label', 'Abrir menu de navegação');
    menu.classList.remove('is-open');
    if (restoreFocus) button.focus();
  };
  button.addEventListener('click', () => {
    const open = !isOpen();
    button.setAttribute('aria-expanded', String(open));
    button.setAttribute('aria-label', open ? 'Fechar menu de navegação' : 'Abrir menu de navegação');
    menu.classList.toggle('is-open', open);
    if (open) menu.querySelector('a')?.focus();
  });
  menu.addEventListener('click', event => { if (event.target.closest('a')) close(); });
  document.addEventListener('keydown', event => { if (event.key === 'Escape') close(true); });
  document.addEventListener('pointerdown', event => {
    if (isOpen() && !menu.contains(event.target) && !button.contains(event.target)) close();
  });
  window.addEventListener('resize', () => { if (window.innerWidth > 980) close(); });
}

const publicHeader = document.querySelector('[data-public-header]');
const syncHeader = () => publicHeader?.classList.toggle('is-scrolled', window.scrollY > 12);
window.addEventListener('scroll', syncHeader, { passive: true }); syncHeader();

export function shouldHandleSmoothScroll(link) {
  if (!(link instanceof HTMLAnchorElement)) return false;
  const rawHref = link.getAttribute('href');
  if (!rawHref || rawHref.startsWith('mailto:') || rawHref.startsWith('tel:')) return false;
  const destination = new URL(link.href, window.location.href);
  if (destination.origin !== window.location.origin || destination.pathname !== window.location.pathname || !destination.hash) return false;
  try { return document.querySelector(destination.hash) !== null; } catch { return false; }
}

document.querySelectorAll('a[href*="#"]').forEach(link => {
  link.addEventListener('click', event => {
    if (!shouldHandleSmoothScroll(link)) return;
    const destination = new URL(link.href, window.location.href);
    const id = destination.hash;
    const target = document.querySelector(id);
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    event.preventDefault();
    target.scrollIntoView({ behavior: 'smooth', block: 'start' });
    history.pushState(null, '', id);
  });
});

document.querySelectorAll('[data-password-toggle]').forEach(toggle => {
  const input = toggle.closest('.of-password-field')?.querySelector('input');
  if (!input) return;
  toggle.addEventListener('click', () => {
    const showing = input.type === 'text';
    input.type = showing ? 'password' : 'text';
    toggle.setAttribute('aria-pressed', String(!showing));
    toggle.setAttribute('aria-label', showing ? 'Mostrar senha' : 'Ocultar senha');
    input.focus();
  });
  const warning = input.closest('.of-field')?.querySelector('[data-caps-lock]');
  input.addEventListener('keyup', event => { if (warning) warning.hidden = !event.getModifierState('CapsLock'); });
});

const summary = document.querySelector('[data-focus-first-error] .of-form-summary:not(:empty)');
if (summary) {
  summary.focus();
  document.querySelector('[aria-invalid="true"]')?.scrollIntoView({ block: 'center' });
}
