(() => {
  const safeInit = () => {
    const input = document.querySelector('[data-command-filter]');
    const items = [...document.querySelectorAll('[data-command-item]')];
    const empty = document.querySelector('[data-command-empty]');
    if (!input || !empty) return;
    input.addEventListener('input', () => {
      const term = input.value.trim().toLocaleLowerCase('pt-BR');
      let visible = 0;
      items.forEach((item) => { const show = !term || item.dataset.searchText.toLocaleLowerCase('pt-BR').includes(term); item.hidden = !show; if(show) visible += 1; });
      empty.hidden = visible !== 0;
    });
  };
  document.readyState === 'loading' ? document.addEventListener('DOMContentLoaded', safeInit, { once:true }) : safeInit();
})();
