(() => {
  let returnFocus;
  const open = (node, trigger) => { if (!node) return; returnFocus = trigger; node.hidden = false; node.querySelector('button,a')?.focus(); };
  const close = (node) => { if (!node) return; node.hidden = true; returnFocus?.focus(); };
  const sheet = document.querySelector('[data-action-sheet]');
  const drawer = document.querySelector('[data-help-drawer]');
  const preserved = document.querySelector('[data-preserved-dialog]');
  document.querySelector('[data-action-open]')?.addEventListener('click', e => open(sheet, e.currentTarget));
  document.querySelector('[data-help-open]')?.addEventListener('click', e => open(drawer, e.currentTarget));
  document.querySelectorAll('[data-preserved-open]').forEach(button => button.addEventListener('click', e => open(preserved, e.currentTarget)));
  document.querySelector('[data-menu-open]')?.addEventListener('click', () => document.body.classList.toggle('menu-open'));
  document.querySelectorAll('[data-dialog-close]').forEach(button => button.addEventListener('click', () => close(button.closest('[role="dialog"]'))));
  document.querySelector('[data-banner-dismiss]')?.addEventListener('click', e => { sessionStorage.setItem('of-plan-banner','dismissed'); e.currentTarget.closest('.of-paused-banner')?.remove(); });
  if (sessionStorage.getItem('of-plan-banner') === 'dismissed') document.querySelector('.of-paused-banner')?.remove();
  document.addEventListener('keydown', e => { if (e.key !== 'Escape') return; if (!sheet?.hidden) close(sheet); else if (!drawer?.hidden) close(drawer); else if (!preserved?.hidden) close(preserved); else document.body.classList.remove('menu-open'); });
})();
document.querySelectorAll('[data-demo-title]').forEach(button => button.addEventListener('click', () => { const dialog=document.querySelector('[data-demo-dialog]'); dialog.querySelector('h2').textContent=button.dataset.demoTitle; dialog.hidden=false; dialog.querySelector('button').focus(); }));
