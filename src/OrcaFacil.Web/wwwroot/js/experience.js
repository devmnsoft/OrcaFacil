(() => {
  let returnFocus = null;

  const focusableSelector = 'button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';
  const open = (node, trigger) => {
    if (!node) return;
    returnFocus = trigger ?? document.activeElement;
    node.hidden = false;
    document.body.classList.add('has-overlay');
    node.querySelector(focusableSelector)?.focus();
  };
  const close = (node) => {
    if (!node) return;
    node.hidden = true;
    document.body.classList.remove('has-overlay');
    returnFocus?.focus();
  };

  const shell = document.querySelector('[data-client-shell]');
  const sidebarToggle = document.querySelector('[data-sidebar-toggle]');
  const sheet = document.querySelector('[data-action-sheet]');
  const drawer = document.querySelector('[data-help-drawer]');
  const search = document.querySelector('[data-search-dialog]');

  if (localStorage.getItem('of-sidebar-collapsed') === 'true') {
    shell?.classList.add('is-collapsed');
    sidebarToggle?.setAttribute('aria-expanded', 'false');
  }

  sidebarToggle?.addEventListener('click', () => {
    const collapsed = shell?.classList.toggle('is-collapsed') ?? false;
    localStorage.setItem('of-sidebar-collapsed', String(collapsed));
    sidebarToggle.setAttribute('aria-expanded', String(!collapsed));
    sidebarToggle.setAttribute('aria-label', collapsed ? 'Expandir menu' : 'Recolher menu');
  });

  document.querySelector('[data-action-open]')?.addEventListener('click', (event) => open(sheet, event.currentTarget));
  document.querySelector('[data-help-open]')?.addEventListener('click', (event) => open(drawer, event.currentTarget));
  document.querySelector('[data-search-open]')?.addEventListener('click', (event) => open(search, event.currentTarget));
  document.querySelector('[data-menu-open]')?.addEventListener('click', () => document.body.classList.toggle('menu-open'));
  document.querySelectorAll('[data-dialog-close]').forEach((button) => button.addEventListener('click', () => close(button.closest('[role="dialog"]'))));

  document.addEventListener('keydown', (event) => {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      open(search, document.querySelector('[data-search-open]'));
    }
    if (event.key === 'Escape') {
      const visibleDialog = document.querySelector('[role="dialog"]:not([hidden])');
      if (visibleDialog) close(visibleDialog);
      else document.body.classList.remove('menu-open');
    }
    if (event.key === 'Tab') {
      const dialog = document.querySelector('[role="dialog"]:not([hidden])');
      if (!dialog) return;
      const focusable = [...dialog.querySelectorAll(focusableSelector)];
      if (!focusable.length) return;
      const first = focusable[0];
      const last = focusable.at(-1);
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    }
  });

  document.querySelectorAll('[data-demo-title]').forEach((button) => {
    button.addEventListener('click', () => {
      const dialog = document.querySelector('[data-demo-dialog]');
      const content = document.querySelector(`[data-demo-content="${button.dataset.demoTitle}"]`);
      const host = dialog?.querySelector('[data-demo-host]');
      if (host && content) host.innerHTML = content.innerHTML;
      open(dialog, button);
    });
  });
})();
