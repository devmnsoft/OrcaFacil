export function safeInitForms(root = document) {
  root.querySelectorAll('form[data-submit-lock]').forEach(form => {
    if (form.dataset.formV59Ready === 'true') return;
    form.dataset.formV59Ready = 'true';
    form.addEventListener('submit', event => {
      if (form.dataset.submitting === 'true') { event.preventDefault(); return; }
      if (!form.checkValidity()) return;
      form.dataset.submitting = 'true';
      form.setAttribute('aria-busy', 'true');
      const submitter = event.submitter;
      if (!submitter) return;
      submitter.dataset.originalLabel = submitter.textContent;
      submitter.setAttribute('aria-disabled', 'true');
      submitter.classList.add('is-loading');
      const loadingLabel = submitter.dataset.loading || 'Processando…';
      submitter.textContent = loadingLabel;
    });
  });
}

if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', () => safeInitForms(), { once: true });
else safeInitForms();
