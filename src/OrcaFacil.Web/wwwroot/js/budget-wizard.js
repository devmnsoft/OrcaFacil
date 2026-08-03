const wizard = document.querySelector('[data-budget-wizard]');
if (wizard) {
  const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
  const token = wizard.querySelector('input[name="__RequestVerificationToken"]')?.value;
  const panels = [...wizard.querySelectorAll('[data-step]')];
  const steps = [...wizard.querySelectorAll('[data-step-target]')];
  const itemHost = wizard.querySelector('[data-budget-items]');
  const previous = wizard.querySelector('[data-step-previous]');
  const next = wizard.querySelector('[data-step-next]');
  const finish = wizard.querySelector('[data-finish]');
  let currentStep = Math.max(0, Math.min(4, Number(wizard.dataset.currentStep) || 0));
  let rowVersion = wizard.dataset.rowVersion;
  let timer;
  let saving = false;
  let queued = false;

  const num = input => Math.max(0, Number.parseFloat(input?.value?.replace(',', '.')) || 0);
  const rows = () => [...itemHost.querySelectorAll('[data-budget-item]')];
  const field = name => wizard.querySelector(`[data-field="${name}"]`);
  const setState = (label, state = 'pending') => { const host = document.querySelector('[data-save-state]'); host.dataset.state = state; host.querySelector('strong').textContent = label; };
  const rename = () => rows().forEach((row, i) => row.querySelectorAll('input[name]').forEach(input => { const prop = input.name.split('.').pop(); input.name = `Items[${i}].${prop}`; }));
  const calculate = () => {
    let subtotal = 0;
    rows().forEach(row => { const total = Math.max(0, num(row.querySelector('[data-item-quantity]')) * num(row.querySelector('[data-item-price]')) - num(row.querySelector('[data-item-discount]'))); subtotal += total; row.querySelector('[data-item-total]').textContent = money.format(total); });
    const total = Math.max(0, subtotal - num(wizard.querySelector('[data-general-discount]')));
    document.querySelectorAll('[data-summary-subtotal]').forEach(x => x.textContent = money.format(subtotal));
    document.querySelectorAll('[data-summary-total], [data-review-total]').forEach(x => x.textContent = money.format(total));
    const count = rows().filter(x => x.querySelector('[data-item-description]').value.trim()).length;
    wizard.querySelector('[data-review-items]').textContent = `${count} ${count === 1 ? 'serviço' : 'serviços'}`;
  };
  const update = () => {
    const client = wizard.querySelector('[data-client-name]').value || 'Não informado';
    wizard.querySelector('[data-review-client]').textContent = client;
    wizard.querySelector('[data-summary-client]').textContent = client === 'Não informado' ? 'Nenhum cliente selecionado' : client;
    wizard.querySelector('[data-review-template]').textContent = wizard.querySelector('input[name="presentation"]:checked')?.closest('label')?.querySelector('strong')?.textContent || 'Essencial';
    calculate();
  };
  const payload = () => ({
    documentId: wizard.dataset.documentId, clientId: wizard.querySelector('[data-client-id]').value || null, currentStep,
    validUntil: field('validUntil')?.value || null, expectedStartAt: field('expectedStartAt')?.value || null,
    estimatedDuration: field('estimatedDuration')?.value || null, paymentMethod: field('paymentMethod')?.value || null,
    installmentCount: num(field('installmentCount')) || null, depositAmount: num(field('depositAmount')), pixInformation: field('pixInformation')?.value || null,
    warrantyText: field('warrantyText')?.value || null, conditionsText: field('conditionsText')?.value || null,
    templateCode: wizard.querySelector('input[name="presentation"]:checked')?.value || 'essential', discount: num(wizard.querySelector('[data-general-discount]')),
    items: rows().map((row, sortOrder) => ({ serviceCatalogItemId: row.dataset.serviceId || null, description: row.querySelector('[data-item-description]').value, unit: row.querySelector('[data-item-unit]').value, quantity: num(row.querySelector('[data-item-quantity]')), unitPrice: num(row.querySelector('[data-item-price]')), discount: num(row.querySelector('[data-item-discount]')), notes: null, sortOrder })),
    rowVersion, idempotencyKey: crypto.randomUUID()
  });
  const save = async (finalize = false) => {
    if (saving) { queued = true; return false; }
    saving = true; setState('Salvando…', 'saving');
    try {
      const response = await fetch(`?handler=${finalize ? 'Finalize' : 'Autosave'}`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token }, body: JSON.stringify(payload()) });
      const data = await response.json();
      if (!response.ok) throw new Error(data.error || 'Não foi possível salvar.');
      if (finalize) { window.location.assign(data.redirectUrl); return true; }
      rowVersion = data.rowVersion; setState('Salvo agora', 'saved'); return true;
    } catch (error) { setState(error.message || 'Falha ao salvar — tentar novamente', 'error'); return false; }
    finally { saving = false; if (queued && !finalize) { queued = false; save(); } }
  };
  const scheduleSave = () => { clearTimeout(timer); setState('Alterações pendentes'); timer = setTimeout(() => save(), 700); };
  const go = step => {
    currentStep = Math.max(0, Math.min(4, step)); panels.forEach((p, i) => { p.hidden = i !== currentStep; p.classList.toggle('is-active', i === currentStep); });
    steps.forEach((b, i) => { b.classList.toggle('is-active', i === currentStep); b.classList.toggle('is-complete', i < currentStep); b.toggleAttribute('aria-current', i === currentStep); });
    previous.hidden = currentStep === 0; next.hidden = currentStep === 4; finish.hidden = currentStep !== 4; update(); scheduleSave();
  };
  const valid = () => {
    if (currentStep === 0 && !wizard.querySelector('[data-client-id]').value) { wizard.querySelector('[data-client-search]').setCustomValidity('Selecione um cliente cadastrado.'); wizard.querySelector('[data-client-search]').reportValidity(); return false; }
    if (currentStep === 1 && !rows().some(x => x.querySelector('[data-item-description]').value.trim())) { rows()[0].querySelector('[data-item-description]').setCustomValidity('Adicione ao menos um serviço.'); rows()[0].querySelector('[data-item-description]').reportValidity(); return false; }
    return true;
  };
  const addItem = (description = '', service = null) => { const row = rows()[0].cloneNode(true); row.querySelectorAll('input').forEach(x => x.value = x.matches('[data-item-quantity]') ? '1' : ''); row.querySelector('[data-item-description]').value = description; if (service) { row.dataset.serviceId = service.id; row.querySelector('[data-item-price]').value = service.price; row.querySelector('[data-item-unit]').value = service.unit; } itemHost.append(row); rename(); update(); scheduleSave(); };
  next.addEventListener('click', () => { if (valid()) go(currentStep + 1); }); previous.addEventListener('click', () => go(currentStep - 1));
  steps.forEach(x => x.addEventListener('click', () => { const target = Number(x.dataset.stepTarget); if (target <= currentStep || valid()) go(target); }));
  wizard.querySelector('[data-item-add]').addEventListener('click', () => addItem()); wizard.querySelectorAll('[data-service-example]').forEach(x => x.addEventListener('click', () => addItem(x.dataset.serviceExample)));
  itemHost.addEventListener('click', event => { const row = event.target.closest('[data-budget-item]'); if (!row) return; if (event.target.closest('[data-item-remove]')) { if (rows().length === 1) row.querySelectorAll('input').forEach(x => x.value = x.matches('[data-item-quantity]') ? '1' : ''); else row.remove(); } if (event.target.closest('[data-item-duplicate]')) row.after(row.cloneNode(true)); rename(); update(); scheduleSave(); });
  wizard.querySelectorAll('[data-client-option]').forEach(option => option.addEventListener('click', () => { wizard.querySelector('[data-client-id]').value = option.dataset.clientId; wizard.querySelector('[data-client-name]').value = option.dataset.clientName; wizard.querySelectorAll('[data-client-option]').forEach(x => x.classList.toggle('is-selected', x === option)); update(); scheduleSave(); }));
  wizard.querySelector('[data-client-search]').addEventListener('input', event => { event.target.setCustomValidity(''); const term = event.target.value.trim().toLocaleLowerCase('pt-BR'); wizard.querySelectorAll('[data-client-option]').forEach(x => x.hidden = !x.dataset.search.includes(term)); });
  wizard.addEventListener('input', event => { event.target.setCustomValidity?.(''); update(); scheduleSave(); }); wizard.addEventListener('change', scheduleSave);
  wizard.querySelector('[data-summary-toggle]').addEventListener('click', event => { const content = wizard.querySelector('[data-summary-content]'); content.hidden = !content.hidden; event.currentTarget.setAttribute('aria-expanded', String(!content.hidden)); });
  wizard.querySelector('[data-preview-link]').addEventListener('click', async event => { event.preventDefault(); if (await save()) window.location.assign(event.currentTarget.href); });
  finish.addEventListener('click', async () => { if (valid()) await save(true); });
  wizard.querySelectorAll('[data-initial-unit]').forEach(x => { x.value = x.dataset.initialUnit || 'serviço'; });
  wizard.querySelectorAll('[data-initial-value]').forEach(x => { if (x.dataset.initialValue) x.value = x.dataset.initialValue; });

  const serviceSearch = wizard.querySelector('[data-service-search]');
  const serviceResults = wizard.querySelector('[data-service-results]');
  let serviceTimer;
  serviceSearch?.addEventListener('input', () => {
    clearTimeout(serviceTimer);
    serviceTimer = setTimeout(async () => {
      const query = serviceSearch.value.trim();
      if (query.length < 2) { serviceResults.replaceChildren(); return; }
      serviceResults.setAttribute('aria-busy', 'true');
      const response = await fetch(`/Internal/Services/Search?q=${encodeURIComponent(query)}&limit=8`);
      const data = response.ok ? await response.json() : { results: [] };
      serviceResults.replaceChildren(...data.results.map(service => {
        const button = document.createElement('button'); button.type = 'button'; button.className = 'of-service-result';
        button.textContent = `${service.name} · ${money.format(service.price)}`;
        button.addEventListener('click', () => addItem(service.description || service.name, service)); return button;
      }));
      serviceResults.setAttribute('aria-busy', 'false');
    }, 250);
  });
  rename(); update(); go(currentStep);
}
