const requestForm = document.querySelector('form[action*="Request"]');
requestForm?.addEventListener('submit', event => {
  const description = requestForm.querySelector('textarea');
  if (!description?.value.trim()) { event.preventDefault(); description?.focus(); return; }
  const submit = requestForm.querySelector('button[type="submit"]');
  if (submit) { submit.disabled = true; submit.textContent = submit.dataset.submitLabel || 'Enviando…'; submit.setAttribute('aria-busy', 'true'); }
});
