const editor = document.querySelector('[data-client-editor]');
const type = editor?.querySelector('[data-person-type]');
const documentInput = editor?.querySelector('[data-document]');
const digits = value => (value || '').replace(/\D/g, '');
const mask = (value, company) => {
  const number = digits(value).slice(0, company ? 14 : 11);
  return company
    ? number.replace(/(\d{2})(\d)/, '$1.$2').replace(/(\d{3})(\d)/, '$1.$2').replace(/(\d{3})(\d)/, '$1/$2').replace(/(\d{4})(\d{1,2})$/, '$1-$2')
    : number.replace(/(\d{3})(\d)/, '$1.$2').replace(/(\d{3})(\d)/, '$1.$2').replace(/(\d{3})(\d{1,2})$/, '$1-$2');
};
const sync = () => {
  const company = type?.value === 'Company';
  editor?.querySelectorAll('[data-company-field]').forEach(field => { field.hidden = !company; });
  const label = editor?.querySelector('[data-document-label]');
  if (label) label.textContent = company ? 'CNPJ' : 'CPF';
  if (documentInput) { documentInput.placeholder = company ? '00.000.000/0000-00' : '000.000.000-00'; documentInput.value = mask(documentInput.value, company); }
};
type?.addEventListener('change', sync);
documentInput?.addEventListener('input', sync);
editor?.addEventListener('submit', event => { const button = event.submitter; if (button) { button.disabled = true; button.setAttribute('aria-busy', 'true'); } });
sync();
