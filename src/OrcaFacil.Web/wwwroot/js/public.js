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
}
