const digits = value => value.replace(/\D/g, '');
const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
const money = value => {
    const raw = digits(value);
    return raw ? new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(Number(raw) / 100) : '';
};

const focusFirstError = () => {
    const summary = document.querySelector('.of-onboarding-summary:not(:empty)');
    if (summary) {
        summary.tabIndex = -1;
        summary.focus({ preventScroll: true });
    }
    const error = document.querySelector('.input-validation-error, .field-validation-error');
    const target = error?.matches('input, select, textarea') ? error : error?.closest('.of-onboarding-field')?.querySelector('input, select, textarea');
    target?.scrollIntoView({ block: 'center', behavior: reducedMotion ? 'auto' : 'smooth' });
    target?.focus({ preventScroll: true });
};

document.querySelectorAll('[data-onboarding-form]').forEach(form => form.addEventListener('submit', event => {
    if (form.dataset.submitting === 'true') {
        event.preventDefault();
        return;
    }
    if (!form.checkValidity()) {
        focusFirstError();
        return;
    }
    form.dataset.submitting = 'true';
    form.querySelectorAll('[type="submit"]').forEach(button => {
        button.disabled = true;
        button.setAttribute('aria-busy', 'true');
        if (button.dataset.submitText) button.textContent = button.dataset.submitText;
    });
}));

document.querySelectorAll('[data-phone]').forEach(input => input.addEventListener('input', () => {
    const value = digits(input.value).slice(0, 11);
    input.value = value.length > 10
        ? value.replace(/(\d{2})(\d{5})(\d{0,4})/, '($1) $2-$3')
        : value.replace(/(\d{2})(\d{4})(\d{0,4})/, '($1) $2-$3');
}));

document.querySelectorAll('[data-money]').forEach(input => input.addEventListener('blur', () => { input.value = money(input.value); }));

const checkMargin = () => {
    const price = document.querySelector('[data-preview-source="price"]');
    const cost = document.querySelector('[data-cost]');
    const warning = document.querySelector('[data-margin-warning]');
    if (!price || !cost || !warning) return;
    const isNegative = Number(digits(cost.value)) > Number(digits(price.value));
    warning.textContent = isNegative ? 'Atenção: o custo informado é maior que o preço.' : '';
    warning.classList.toggle('margin-negative', isNegative);
};

document.querySelectorAll('[data-preview-source]').forEach(input => input.addEventListener('input', () => {
    const target = document.querySelector(`[data-preview-target="${CSS.escape(input.dataset.previewSource)}"]`);
    if (target) target.textContent = input.hasAttribute('data-money') ? money(input.value) : input.value || '—';
    checkMargin();
}));

document.querySelectorAll('[data-person-type]').forEach(select => select.addEventListener('change', () => {
    const documentInput = select.closest('form')?.querySelector('[data-document]');
    const label = documentInput ? document.querySelector(`label[for="${CSS.escape(documentInput.id)}"]`) : null;
    if (label) label.textContent = select.value === '1' ? 'CNPJ' : 'CPF';
    if (documentInput) documentInput.placeholder = select.value === '1' ? 'Ex.: 12.345.678/0001-90' : 'Ex.: 123.456.789-00';
}));

focusFirstError();
