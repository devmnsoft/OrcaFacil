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

const shareCenter = document.querySelector('[data-share-center]');
if (shareCenter) {
  const message = shareCenter.querySelector('[data-share-message]');
  const encode = () => encodeURIComponent(message.value);
  const update = () => {
    shareCenter.querySelector('[data-share-whatsapp]').href = `https://wa.me/?text=${encode()}`;
    shareCenter.querySelector('[data-share-email]').href = `mailto:?subject=${encodeURIComponent('Proposta OrçaFácil')}&body=${encode()}`;
  };
  message.addEventListener('input', update); update();
  shareCenter.querySelector('[data-copy-message]')?.addEventListener('click', async (event) => {
    await navigator.clipboard.writeText(message.value);
    event.currentTarget.textContent = 'Mensagem copiada';
  });
}
