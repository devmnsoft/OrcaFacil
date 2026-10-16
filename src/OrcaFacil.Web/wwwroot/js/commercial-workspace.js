document.querySelectorAll('[data-copy]').forEach((copyButton) => {
  copyButton.addEventListener('click', async () => {
    copyButton.disabled = true;
    try {
      await navigator.clipboard.writeText(copyButton.dataset.copy);
      copyButton.textContent = 'Link copiado';
    } catch {
      copyButton.textContent = 'Não foi possível copiar';
    } finally {
      window.setTimeout(() => { copyButton.disabled = false; }, 1200);
    }
  });
});

document.querySelectorAll('.of-command-rail form').forEach((form) => {
  form.addEventListener('submit', () => {
    const button = form.querySelector('button[type="submit"]');
    if (button) { button.disabled = true; button.setAttribute('aria-busy', 'true'); button.textContent = button.dataset.loadingText || button.textContent; }
  });
});

document.querySelectorAll('[data-follow-up-days]').forEach((button) => button.addEventListener('click', () => {
  const input = button.closest('form')?.querySelector('input[type="datetime-local"]');
  if (!input) return;
  const date = new Date();
  date.setDate(date.getDate() + Number(button.dataset.followUpDays));
  date.setHours(9, 0, 0, 0);
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  input.value = local.toISOString().slice(0, 16);
  input.focus();
}));

const commandRail = document.querySelector('.of-command-rail');
const commandToggle = document.querySelector('[data-command-toggle]');
const setCommandRailExpanded = (expanded) => {
  if (!commandRail || !commandToggle) return;
  commandRail.classList.toggle('is-expanded', expanded);
  commandToggle.setAttribute('aria-expanded', String(expanded));
  commandToggle.textContent = expanded ? 'Fechar ações' : 'Mais ações';
};
if (commandRail && commandToggle) {
  commandRail.classList.add('is-collapsible');
  setCommandRailExpanded(false);
}
commandToggle?.addEventListener('click', () => {
  setCommandRailExpanded(!commandRail?.classList.contains('is-expanded'));
});
commandRail?.addEventListener('keydown', (event) => {
  if (event.key !== 'Escape' || !commandRail.classList.contains('is-expanded')) return;
  setCommandRailExpanded(false);
  commandToggle?.focus({ preventScroll: true });
});

const shareCenter = document.querySelector('[data-share-center]');
if (shareCenter) {
  const message = shareCenter.querySelector('[data-share-message]');
  const encode = () => encodeURIComponent(message.value);
  const update = () => {
    shareCenter.querySelector('[data-share-whatsapp]').href = `https://wa.me/?text=${encode()}`;
    shareCenter.querySelector('[data-share-email]').href = `mailto:?subject=${encodeURIComponent('Proposta Fechô')}&body=${encode()}`;
  };
  message.addEventListener('input', update); update();
  shareCenter.querySelector('[data-copy-message]')?.addEventListener('click', async (event) => {
    await navigator.clipboard.writeText(message.value);
    event.currentTarget.textContent = 'Mensagem copiada';
  });
}

const tabList = document.querySelector('[data-workspace-tabs]');
if (tabList) {
  const tabs = Array.from(tabList.querySelectorAll('[role="tab"][data-tab-target]'));
  const panels = new Map(Array.from(document.querySelectorAll('[data-tab-panel]')).map((panel) => [panel.dataset.tabPanel, panel]));
  const aliases = { sharing: 'negotiation', receipt: 'finance', receipts: 'finance', timeline: 'history' };
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const params = new URLSearchParams(window.location.search);
  const normalize = (value) => aliases[value?.toLowerCase()] || value?.toLowerCase();
  const firstTab = tabs[0]?.dataset.tabTarget;
  const requestedRawTab = params.get('tab')?.toLowerCase();
  const requestedTab = normalize(requestedRawTab);

  const focusCommandSection = (name) => {
    const section = document.querySelector(`[data-command-section="${name}"]`);
    if (!section) return;
    setCommandRailExpanded(true);
    window.requestAnimationFrame(() => {
      section.setAttribute('tabindex', '-1');
      section.scrollIntoView({ block: 'nearest', behavior: reducedMotion ? 'auto' : 'smooth' });
      section.focus({ preventScroll: true });
    });
  };

  const activate = (name, options = {}) => {
    const targetName = panels.has(name) ? name : firstTab;
    if (!targetName) return;
    tabs.forEach((tab) => {
      const selected = tab.dataset.tabTarget === targetName;
      tab.setAttribute('aria-selected', String(selected));
      tab.tabIndex = selected ? 0 : -1;
    });
    panels.forEach((panel, panelName) => {
      panel.hidden = panelName !== targetName;
    });
    if (options.updateUrl) {
      const nextUrl = new URL(window.location.href);
      nextUrl.searchParams.set('tab', targetName);
      window.history.replaceState(null, '', nextUrl);
    }
    if (options.focusPanel) {
      const panel = panels.get(targetName);
      panel?.setAttribute('tabindex', '-1');
      panel?.focus({ preventScroll: true });
    }
    if (options.scroll) {
      tabList.scrollIntoView({ block: 'start', behavior: reducedMotion ? 'auto' : 'smooth' });
    }
  };

  tabs.forEach((tab, index) => {
    tab.addEventListener('click', () => activate(tab.dataset.tabTarget, { updateUrl: true, focusPanel: true }));
    tab.addEventListener('keydown', (event) => {
      const lastIndex = tabs.length - 1;
      const direction = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0;
      if (direction !== 0) {
        event.preventDefault();
        const next = tabs[(index + direction + tabs.length) % tabs.length];
        next.focus();
        activate(next.dataset.tabTarget, { updateUrl: true });
      } else if (event.key === 'Home' || event.key === 'End') {
        event.preventDefault();
        const next = event.key === 'Home' ? tabs[0] : tabs[lastIndex];
        next.focus();
        activate(next.dataset.tabTarget, { updateUrl: true });
      }
    });
  });

  activate(requestedTab || firstTab, { focusPanel: Boolean(requestedTab), scroll: Boolean(requestedTab) });
  if (requestedRawTab === 'sharing') focusCommandSection('sharing');
}
