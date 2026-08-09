const copyButton = document.querySelector('[data-copy]');
copyButton?.addEventListener('click', async () => {
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

document.querySelectorAll('.of-command-rail form').forEach((form) => {
  form.addEventListener('submit', () => {
    const button = form.querySelector('button[type="submit"]');
    if (button) { button.disabled = true; button.setAttribute('aria-busy', 'true'); }
  });
});
