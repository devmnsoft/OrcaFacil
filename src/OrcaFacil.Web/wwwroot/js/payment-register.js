const form = document.querySelector('[data-payment-form]');

if (form) {
  form.querySelector('.field-validation-error, [aria-invalid="true"]')?.focus();
  form.addEventListener('submit', event => {
    if (!form.checkValidity()) {
      event.preventDefault();
      form.reportValidity();
      form.querySelector(':invalid')?.focus();
      return;
    }
    const button = event.submitter;
    if (!button || button.disabled) {
      event.preventDefault();
      return;
    }
    button.disabled = true;
    button.setAttribute('aria-busy', 'true');
    button.textContent = 'Registrando…';
  });
}
