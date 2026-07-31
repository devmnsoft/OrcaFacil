const wizard = document.querySelector('[data-budget-wizard]');

if (wizard) {
  const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
  const panels = [...wizard.querySelectorAll('[data-step]')];
  const stepButtons = [...wizard.querySelectorAll('[data-step-target]')];
  const previous = wizard.querySelector('[data-step-previous]');
  const next = wizard.querySelector('[data-step-next]');
  const finish = wizard.querySelector('[data-finish]');
  const itemHost = wizard.querySelector('[data-budget-items]');
  const preview = document.querySelector('[data-budget-preview]');
  let currentStep = 0;

  const number = (input) => Math.max(0, Number.parseFloat(input?.value?.replace(',', '.')) || 0);
  const itemRows = () => [...itemHost.querySelectorAll('[data-budget-item]')];

  const setSaveState = (label, state = 'pending') => {
    const host = document.querySelector('[data-save-state]');
    if (!host) return;
    host.dataset.state = state;
    host.querySelector('strong').textContent = label;
  };

  const renameItems = () => itemRows().forEach((row, index) => {
    const names = ['Description', 'Quantity', 'UnitPrice', 'Discount'];
    row.querySelectorAll('input[name]').forEach((input, inputIndex) => {
      input.name = `Input.Items[${index}].${names[inputIndex]}`;
    });
  });

  const calculate = () => {
    let subtotal = 0;
    itemRows().forEach((row) => {
      const total = Math.max(0, number(row.querySelector('[data-item-quantity]')) * number(row.querySelector('[data-item-price]')) - number(row.querySelector('[data-item-discount]')));
      subtotal += total;
      row.querySelector('[data-item-total]').textContent = money.format(total);
    });
    const total = Math.max(0, subtotal - number(wizard.querySelector('[data-general-discount]')));
    document.querySelectorAll('[data-summary-subtotal]').forEach((node) => { node.textContent = money.format(subtotal); });
    document.querySelectorAll('[data-summary-total], [data-review-total], [data-preview-total]').forEach((node) => { node.textContent = money.format(total); });
    const completed = itemRows().filter((row) => row.querySelector('[data-item-description]').value.trim()).length;
    wizard.querySelector('[data-review-items]').textContent = `${completed} ${completed === 1 ? 'serviço' : 'serviços'}`;
  };

  const updateReview = () => {
    const client = wizard.querySelector('[data-client-name]').value.trim() || 'Não informado';
    const template = wizard.querySelector('input[name="presentation"]:checked')?.value || 'Essencial';
    wizard.querySelector('[data-review-client]').textContent = client;
    wizard.querySelector('[data-review-template]').textContent = template;
    wizard.querySelector('[data-summary-client]').textContent = client === 'Não informado' ? 'Nenhum cliente selecionado' : client;
    document.querySelector('[data-preview-client]').textContent = client;
    calculate();
  };

  const goTo = (step) => {
    currentStep = Math.max(0, Math.min(panels.length - 1, step));
    panels.forEach((panel, index) => { panel.hidden = index !== currentStep; panel.classList.toggle('is-active', index === currentStep); });
    stepButtons.forEach((button, index) => {
      button.classList.toggle('is-active', index === currentStep);
      button.classList.toggle('is-complete', index < currentStep);
      if (index === currentStep) button.setAttribute('aria-current', 'step'); else button.removeAttribute('aria-current');
    });
    previous.hidden = currentStep === 0;
    next.hidden = currentStep === panels.length - 1;
    finish.hidden = currentStep !== panels.length - 1;
    updateReview();
    panels[currentStep].querySelector('h2')?.focus({ preventScroll: true });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const validateStep = () => {
    if (currentStep === 0 && !wizard.querySelector('[data-client-name]').value.trim()) {
      wizard.querySelector('[data-client-name]').setCustomValidity('Informe quem receberá o orçamento.');
      wizard.querySelector('[data-client-name]').reportValidity();
      return false;
    }
    if (currentStep === 1 && !itemRows().some((row) => row.querySelector('[data-item-description]').value.trim())) {
      itemRows()[0]?.querySelector('[data-item-description]').setCustomValidity('Adicione pelo menos um serviço ou produto.');
      itemRows()[0]?.querySelector('[data-item-description]').reportValidity();
      return false;
    }
    return true;
  };

  const newItem = (description = '') => {
    const source = itemRows()[0];
    const row = source.cloneNode(true);
    row.querySelectorAll('input').forEach((input) => { input.value = input.matches('[data-item-quantity]') ? '1' : ''; });
    row.querySelector('[data-item-description]').value = description;
    itemHost.append(row);
    renameItems();
    calculate();
    row.querySelector('[data-item-description]').focus();
  };

  next.addEventListener('click', () => { if (validateStep()) goTo(currentStep + 1); });
  previous.addEventListener('click', () => goTo(currentStep - 1));
  stepButtons.forEach((button) => button.addEventListener('click', () => { const target = Number(button.dataset.stepTarget); if (target <= currentStep || validateStep()) goTo(target); }));
  wizard.querySelector('[data-item-add]').addEventListener('click', () => newItem());
  wizard.querySelectorAll('[data-service-example]').forEach((button) => button.addEventListener('click', () => { newItem(button.dataset.serviceExample); }));

  itemHost.addEventListener('click', (event) => {
    const row = event.target.closest('[data-budget-item]');
    if (!row) return;
    if (event.target.closest('[data-item-remove]')) {
      if (itemRows().length === 1) row.querySelectorAll('input').forEach((input) => { input.value = input.matches('[data-item-quantity]') ? '1' : ''; });
      else row.remove();
      renameItems(); calculate();
    }
    if (event.target.closest('[data-item-duplicate]')) {
      const clone = row.cloneNode(true); row.after(clone); renameItems(); calculate();
    }
  });

  wizard.addEventListener('input', (event) => {
    event.target.setCustomValidity?.('');
    setSaveState('Alterações pendentes');
    updateReview();
  });
  wizard.addEventListener('change', (event) => {
    if (event.target.name === 'presentation') wizard.querySelectorAll('.of-template-option').forEach((option) => option.classList.toggle('is-selected', option.contains(event.target)));
    updateReview();
  });
  wizard.addEventListener('submit', () => setSaveState('Salvando…', 'saving'));

  wizard.querySelector('[data-summary-toggle]').addEventListener('click', (event) => {
    const content = wizard.querySelector('[data-summary-content]');
    content.hidden = !content.hidden;
    event.currentTarget.setAttribute('aria-expanded', String(!content.hidden));
  });

  const renderPreview = () => {
    updateReview();
    const host = preview.querySelector('[data-preview-items]');
    host.replaceChildren(...itemRows().filter((row) => row.querySelector('[data-item-description]').value.trim()).map((row) => {
      const line = document.createElement('div');
      const description = document.createElement('span');
      const value = document.createElement('strong');
      description.textContent = row.querySelector('[data-item-description]').value;
      value.textContent = row.querySelector('[data-item-total]').textContent;
      line.append(description, value);
      return line;
    }));
    preview.showModal();
  };
  wizard.querySelector('[data-preview-open]').addEventListener('click', renderPreview);
  preview.querySelectorAll('[data-preview-close]').forEach((button) => button.addEventListener('click', () => preview.close()));
  preview.querySelector('[data-preview-finish]').addEventListener('click', () => { preview.close(); goTo(4); });
  preview.querySelector('[data-preview-zoom]').addEventListener('input', (event) => { preview.querySelector('[data-preview-page]').style.setProperty('--preview-scale', event.target.value / 100); });

  renameItems();
  calculate();
}
