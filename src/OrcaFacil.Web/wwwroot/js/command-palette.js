const escapeHtml = (value) => String(value).replace(/[&<>'"]/g, (character) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[character]);

export function initializeCommandPalette() {
  const input = document.querySelector('[data-global-search]');
  const host = document.querySelector('[data-search-results]');
  if (!input || !host) return;

  let timer;
  let request;
  let activeIndex = -1;
  const select = (index) => {
    const links = [...host.querySelectorAll('a')];
    if (!links.length) return;
    activeIndex = (index + links.length) % links.length;
    links.forEach((link, itemIndex) => link.toggleAttribute('data-active', itemIndex === activeIndex));
    links[activeIndex].focus();
  };

  input.addEventListener('input', () => {
    window.clearTimeout(timer);
    request?.abort();
    const query = input.value.trim();
    if (query.length < 2) {
      host.innerHTML = '<p>Digite ao menos dois caracteres para buscar nesta conta.</p>';
      host.setAttribute('aria-busy', 'false');
      return;
    }
    host.innerHTML = '<p>Buscando…</p>';
    host.setAttribute('aria-busy', 'true');
    timer = window.setTimeout(async () => {
      request = new AbortController();
      try {
        const response = await fetch(`/Internal/Search?q=${encodeURIComponent(query)}&limit=12`, { signal: request.signal, headers: { Accept: 'application/json' } });
        if (!response.ok) throw new Error('search');
        const { results } = await response.json();
        host.innerHTML = results.length
          ? `<p>${results.length} resultado(s) nesta conta</p>${results.map((item) => `<a href="${escapeHtml(item.url)}"><svg class="of-icon" aria-hidden="true"><use href="/img/icons/orcafacil-icons.svg#icon-${escapeHtml(item.icon)}"></use></svg><span><strong>${escapeHtml(item.title)}</strong><small>${escapeHtml(item.type)} · ${escapeHtml(item.subtitle)} · ${escapeHtml(item.status)}</small></span><span>${escapeHtml(item.action)}</span></a>`).join('')}`
          : '<p>Nenhum resultado encontrado nesta conta.</p>';
        activeIndex = -1;
      } catch (error) {
        if (error.name !== 'AbortError') host.innerHTML = '<p role="alert">Não foi possível buscar agora. Tente novamente.</p>';
      } finally {
        host.setAttribute('aria-busy', 'false');
      }
    }, 300);
  });

  input.addEventListener('keydown', (event) => {
    if (event.key === 'ArrowDown') { event.preventDefault(); select(activeIndex + 1); }
    if (event.key === 'ArrowUp') { event.preventDefault(); select(activeIndex - 1); }
    if (event.key === 'Enter' && activeIndex >= 0) { event.preventDefault(); host.querySelectorAll('a')[activeIndex]?.click(); }
  });
}
