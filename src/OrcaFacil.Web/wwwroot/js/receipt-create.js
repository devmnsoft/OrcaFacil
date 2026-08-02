const form = document.querySelector('#receipt-form');

if (form) {
    const output = (name, value) => {
        const element = form.querySelector(`[data-output="${name}"]`);
        if (element) element.textContent = value;
    };

    const update = () => {
        const client = form.querySelector('[data-preview="client"]');
        output('client', client?.selectedOptions[0]?.text || 'cliente selecionado');
        const amount = Number(form.querySelector('[data-preview="amount"]')?.value || 0);
        output('amount', amount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }));
        output('service', form.querySelector('[data-preview="service"]')?.value || 'descrição do serviço');
        output('date', form.querySelector('[data-preview="date"]')?.value?.split('-').reverse().join('/') || '—');
        output('method', form.querySelector('[name="Input.PaymentMethod"]:checked')?.nextElementSibling?.nextElementSibling?.textContent || '—');
        const origin = form.querySelector('[name="Input.OriginType"]:checked')?.value;
        form.querySelectorAll('.origin-dependent').forEach(element => {
            element.hidden = element.dataset.origin !== origin;
        });
    };

    form.addEventListener('input', update);
    form.addEventListener('change', update);
    form.addEventListener('submit', event => {
        if (form.dataset.submitting === 'true') {
            event.preventDefault();
            return;
        }
        if (!form.checkValidity()) {
            form.querySelector(':invalid')?.focus();
            return;
        }
        form.dataset.submitting = 'true';
        form.setAttribute('aria-busy', 'true');
        const submit = form.querySelector('[type="submit"]');
        if (submit) {
            submit.disabled = true;
            submit.textContent = 'Registrando…';
        }
    });
    update();
}
