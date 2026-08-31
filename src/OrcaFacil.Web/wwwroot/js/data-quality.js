(() => {
  'use strict';
  const safeInit = (name, initialize) => {
    try { initialize(); } catch (error) { console.error(`[${name}] componente indisponível`, error); }
  };
  safeInit('data-quality', () => {
    const list = document.querySelector('.quality-list');
    if (!list) return;
    list.addEventListener('click', event => {
      const link = event.target.closest('a[href]');
      if (link) link.setAttribute('aria-busy', 'true');
    });
  });
})();
