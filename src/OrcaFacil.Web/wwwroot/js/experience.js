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
  const menuButton = document.querySelector('[data-menu-open]');
  const sidebar = document.querySelector('#client-sidebar');
  const stage = document.querySelector('.of-shell-stage');
  const mobileMenu = window.matchMedia('(max-width: 1023px)');
  const menuFocusSelector = 'a[href], button, input, select, textarea, summary, [tabindex], [contenteditable="true"]';
  const menuOriginals = new WeakMap();
  const rememberMenuFocus = root => root?.querySelectorAll(menuFocusSelector).forEach(el => {
    if (!menuOriginals.has(el)) menuOriginals.set(el, { had: el.hasAttribute('tabindex'), value: el.getAttribute('tabindex') });
  });
  const restoreMenuFocus = el => {
    const saved = menuOriginals.get(el);
    if (!saved) return;
    if (saved.had) el.setAttribute('tabindex', saved.value);
    else el.removeAttribute('tabindex');
  };
  const tabbableIn = root => [...root.querySelectorAll(menuFocusSelector)].filter(el => !el.hasAttribute('disabled') && el.tabIndex >= 0);
  const applyMenuFocus = open => {
    const modal = open && mobileMenu.matches;
    if (sidebar) sidebar.setAttribute('aria-hidden', String(mobileMenu.matches && !open));
    if ('inert' in HTMLElement.prototype && sidebar) {
      sidebar.inert = mobileMenu.matches && !open;
      if (stage) stage.inert = modal;
      return;
    }
    if (sidebar) {
      rememberMenuFocus(sidebar);
      sidebar.querySelectorAll(menuFocusSelector).forEach(el => { if (mobileMenu.matches && !open) el.setAttribute('tabindex', '-1'); else restoreMenuFocus(el); });
    }
    if (stage) {
      rememberMenuFocus(stage);
      stage.querySelectorAll(menuFocusSelector).forEach(el => { if (modal) el.setAttribute('tabindex', '-1'); else restoreMenuFocus(el); });
    }
  };
  const setMenuOpen = (isOpen, restoreFocus = false) => {
    const open = isOpen && mobileMenu.matches;
    document.body.classList.toggle('menu-open', open);
    menuButton?.setAttribute('aria-expanded', String(open));
    applyMenuFocus(open);
    if (open) tabbableIn(sidebar || document.body)[0]?.focus({ preventScroll: true });
    else if (restoreFocus) menuButton?.focus({ preventScroll: true });
  };
  menuButton?.setAttribute('aria-expanded', 'false');
  menuButton?.setAttribute('aria-controls', 'client-sidebar');
  applyMenuFocus(false);
  menuButton?.addEventListener('click', () => setMenuOpen(!document.body.classList.contains('menu-open'), true));
  sidebar?.addEventListener('click', event => { if (event.target.closest('a[href]')) setMenuOpen(false, true); });
  document.addEventListener('pointerdown', event => {
    if (document.body.classList.contains('menu-open') && sidebar && !sidebar.contains(event.target) && !menuButton?.contains(event.target)) setMenuOpen(false, true);
  });
  document.querySelectorAll('[data-dialog-close]').forEach((button) => button.addEventListener('click', () => close(button.closest('[role="dialog"]'))));

  document.addEventListener('keydown', (event) => {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      open(search, document.querySelector('[data-search-open]'));
    }
    if (event.key === 'Escape') {
      const visibleDialog = document.querySelector('[role="dialog"]:not([hidden])');
      if (visibleDialog) close(visibleDialog);
      else if (document.body.classList.contains('menu-open')) setMenuOpen(false, true);
    }
    if (event.key === 'Tab' && document.body.classList.contains('menu-open') && sidebar && !document.querySelector('[role="dialog"]:not([hidden])')) {
      const items = tabbableIn(sidebar);
      if (items.length) {
        const first = items[0];
        const last = items[items.length - 1];
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
      }
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

  mobileMenu.addEventListener('change', () => { if (!mobileMenu.matches) setMenuOpen(false, sidebar?.contains(document.activeElement)); });

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
