const button = document.querySelector('[data-public-menu-button]');
const menu = document.querySelector('[data-public-menu]');
if (button && menu) {
  const close = () => { button.setAttribute('aria-expanded', 'false'); menu.classList.remove('is-open'); };
  button.addEventListener('click', () => {
    const open = button.getAttribute('aria-expanded') !== 'true';
    button.setAttribute('aria-expanded', String(open));
    menu.classList.toggle('is-open', open);
  });
  menu.addEventListener('click', event => { if (event.target.closest('a')) close(); });
  document.addEventListener('keydown', event => { if (event.key === 'Escape') { close(); button.focus(); } });
  document.addEventListener('pointerdown', event => {
    if (!menu.contains(event.target) && !button.contains(event.target)) close();
  });
}

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
  input.addEventListener('keyup', event => {
    if (warning) warning.hidden = !event.getModifierState('CapsLock');
  });
});

const summary = document.querySelector('[data-focus-first-error] .of-form-summary:not(:empty)');
if (summary) {
  summary.focus();
  document.querySelector('[aria-invalid="true"]')?.scrollIntoView({ block: 'center' });
}
