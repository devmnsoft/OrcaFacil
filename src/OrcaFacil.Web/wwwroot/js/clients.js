const dialog = document.querySelector('[data-delete-dialog]');
let trigger = null;
document.addEventListener('click', event => {
  const button = event.target.closest('[data-delete-client]');
  if (button && dialog) { trigger = button; dialog.querySelector('[data-delete-name]').textContent = button.dataset.clientName; dialog.querySelector('[name="id"]').value = button.dataset.clientId; dialog.showModal(); dialog.querySelector('[data-dialog-cancel]').focus(); }
  if (event.target.closest('[data-dialog-cancel]')) { dialog.close(); trigger?.focus(); }
});
dialog?.addEventListener('close', () => trigger?.focus());
dialog?.addEventListener('cancel', () => trigger?.focus());
dialog?.querySelector('[data-delete-form]')?.addEventListener('submit', event => { const button = event.submitter; if (button?.disabled) event.preventDefault(); else if (button) { button.disabled = true; button.textContent = 'Removendo…'; } });

document.querySelectorAll('[data-client-action-form]').forEach(form => form.addEventListener('submit', event => {
  const button = event.submitter;
  if (!button || button.disabled) return;
  button.disabled = true;
  button.setAttribute('aria-busy', 'true');
}));
