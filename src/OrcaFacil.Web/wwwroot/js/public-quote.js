const form = document.querySelector('[data-submit-once]');
document.querySelector('[data-print-quote]')?.addEventListener('click', () => window.print());

if (form) {
  const firstError = form.closest('section')?.querySelector('.field-validation-error:not(:empty)');
  if (firstError) {
    firstError.setAttribute('tabindex', '-1');
    firstError.focus();
  }

  form.addEventListener('submit', (event) => {
    const submitter = event.submitter;
    if (!submitter || form.dataset.submitting === 'true') {
      event.preventDefault();
      return;
    }
    form.dataset.submitting = 'true';
    submitter.setAttribute('aria-busy', 'true');
    submitter.textContent = 'Registrando…';
    form.querySelectorAll('button').forEach((button) => { if (button !== submitter) button.disabled = true; });
  });
}
