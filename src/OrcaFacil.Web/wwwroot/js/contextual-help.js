const escapeHtml = (value) => String(value ?? '').replace(/[&<>'"]/g, (character) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[character]);

export function initializeContextualHelp() {
  const trigger = document.querySelector('[data-help-open]');
  const drawer = document.querySelector('[data-help-drawer]');
  const title = drawer?.querySelector('[data-help-title]');
  const host = drawer?.querySelector('[data-help-content]');
  if (!trigger || !drawer || !title || !host) return;

  trigger.addEventListener('click', async () => {
    title.textContent = 'Carregando ajuda…';
    host.innerHTML = '<p>Buscando orientações específicas para esta etapa.</p>';
    host.setAttribute('aria-busy', 'true');
    try {
      const response = await fetch(`/Internal/Help/${encodeURIComponent(trigger.dataset.helpCode)}`, { headers: { Accept: 'application/json' } });
      if (!response.ok) throw new Error('help');
      const help = await response.json();
      title.textContent = help.title;
      host.innerHTML = `<section><h3>Para que serve</h3><p>${escapeHtml(help.explanation)}</p></section>
        <section><h3>Quando usar</h3><p>${escapeHtml(help.whenToUse)}</p></section>
        <section><h3>Antes de começar</h3><p>${escapeHtml(help.beforeStarting)}</p></section>
        <section><h3>Passo a passo</h3><ol>${help.steps.map((step) => `<li>${escapeHtml(step)}</li>`).join('')}</ol></section>
        <section><h3>Exemplo</h3><p>${escapeHtml(help.example)}</p></section>
        <section><h3>Erros comuns</h3><ul>${help.commonErrors.map((error) => `<li>${escapeHtml(error)}</li>`).join('')}</ul></section>
        <section><h3>O que acontece depois</h3><p>${escapeHtml(help.whatHappensNext)}</p></section>
        ${help.requiredPlanCode ? `<p class="of-plan-pill">Plano necessário: ${escapeHtml(help.requiredPlanCode)}</p>` : ''}
        <a href="${escapeHtml(help.relatedAction)}">Ir para a ação relacionada</a>`;
    } catch {
      title.textContent = 'Ajuda indisponível';
      host.innerHTML = '<p role="alert">Não foi possível carregar as orientações agora. Tente novamente.</p><a href="/Support">Abrir Central de ajuda</a>';
    } finally {
      host.setAttribute('aria-busy', 'false');
    }
  });
}
