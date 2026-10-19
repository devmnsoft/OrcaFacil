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

  initializeHelpAsk(trigger);
}

/**
 * Pergunta livre sobre a tela atual (E-3). Usa o endpoint governado de RAG:
 * resposta só com base em guias/artigos publicados, sem executar ações.
 */
function initializeHelpAsk(trigger) {
  const form = document.querySelector('[data-help-ask-form]');
  const input = document.querySelector('[data-help-ask-question]');
  const answer = document.querySelector('[data-help-ask-answer]');
  const submit = document.querySelector('[data-help-ask-submit]');
  if (!form || !input || !answer || !submit) return;

  const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    const question = input.value.trim();
    if (question.length < 3) {
      answer.innerHTML = '<p role="alert">Descreva sua dúvida com pelo menos 3 caracteres.</p>';
      input.focus();
      return;
    }

    submit.disabled = true;
    input.disabled = true;
    answer.setAttribute('aria-busy', 'true');
    answer.innerHTML = '<p>Consultando os guias e artigos publicados…</p>';

    try {
      const response = await fetch('/Internal/Help/Ask', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token(), Accept: 'application/json' },
        body: JSON.stringify({ question, helpCode: trigger?.dataset.helpCode ?? '' }),
      });
      const data = await response.json().catch(() => null);
      if (!response.ok || !data) {
        answer.innerHTML = '<p role="alert">Não foi possível consultar a ajuda agora. Tente novamente.</p>';
        return;
      }

      if (!data.succeeded) {
        answer.innerHTML = `<p>${escapeHtml(data.answer || 'Não encontrei informação suficiente nas fontes publicadas.')}</p>
          ${(data.limitations ?? []).map((item) => `<p class="of-help-ask__note">${escapeHtml(item)}</p>`).join('')}`;
        return;
      }

      const sources = (data.sources ?? []).length
        ? `<h4>Fontes consultadas</h4><ul>${data.sources.map((source) =>
            `<li><a href="${escapeHtml(source.url)}">${escapeHtml(source.title)}</a></li>`).join('')}</ul>`
        : '';
      const limitations = (data.limitations ?? []).map((item) => `<li>${escapeHtml(item)}</li>`).join('');

      answer.innerHTML = `<div class="of-help-ask__answer">
          <p>${escapeHtml(data.answer)}</p>
          ${sources}
          ${limitations ? `<h4>Limites desta resposta</h4><ul>${limitations}</ul>` : ''}
          <p class="of-help-ask__note">${escapeHtml(data.notice)}</p>
        </div>`;
    } catch {
      answer.innerHTML = '<p role="alert">Falha de comunicação com a ajuda. Tente novamente.</p>';
    } finally {
      submit.disabled = false;
      input.disabled = false;
      answer.setAttribute('aria-busy', 'false');
    }
  });
}
