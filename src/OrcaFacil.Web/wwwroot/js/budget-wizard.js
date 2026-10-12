const wizard = document.querySelector('[data-budget-wizard]');
if (wizard) {
  const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
  const token = wizard.querySelector('input[name="__RequestVerificationToken"]')?.value;
  const panels = [...wizard.querySelectorAll('[data-step]')];
  const steps = [...wizard.querySelectorAll('[data-step-target]')];
  const itemHost = wizard.querySelector('[data-budget-items]');
  const clientChoices = wizard.querySelector('[data-client-choices]');
  const previous = wizard.querySelector('[data-step-previous]');
  const next = wizard.querySelector('[data-step-next]');
  const finish = wizard.querySelector('[data-finish]');
  let currentStep = Math.max(0, Math.min(4, Number(wizard.dataset.currentStep) || 0));
  let rowVersion = wizard.dataset.rowVersion;
  let idempotencyKey = crypto.randomUUID();
  let confirmPriceChanges = false;
  let timer;
  let saving = false;
  let queued = false;
  let clientSearchController;
  let clientSearchSeq = 0;
  let serviceSearchController;
  let serviceSearchSeq = 0;

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
    rowVersion, idempotencyKey, confirmPriceChanges
  });
  const save = async (finalize = false) => {
    if (saving) { queued = true; return false; }
    saving = true; setState('Salvando…', 'saving');
    try {
      const response = await fetch(`?handler=${finalize ? 'Finalize' : 'Autosave'}`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token }, body: JSON.stringify(payload()) });
      const data = await response.json();
      if (!response.ok) {
        const message = data.error || 'Não foi possível salvar.';
        if (response.status === 409) {
          setState(message + ' Recarregue antes de tentar de novo.', 'error');
          return false;
        }
        if (message.includes('Confirme a alteração de preço')) {
          confirmPriceChanges = false;
          setState(message, 'error');
          let confirm = wizard.querySelector('[data-confirm-price]');
          if (!confirm) {
            confirm = document.createElement('button');
            confirm.type = 'button';
            confirm.className = 'of-button-secondary';
            confirm.dataset.confirmPrice = 'true';
            confirm.textContent = 'Confirmar alteração de preço e salvar';
            wizard.querySelector('[data-save-state]')?.after(confirm);
            confirm.addEventListener('click', () => { confirmPriceChanges = true; save(); });
          }
          confirm.hidden = false;
          return false;
        }
        throw new Error(message);
      }
      if (finalize) { window.location.assign(data.redirectUrl); return true; }
      const draft = data.draft || data;
      rowVersion = draft.rowVersion || data.rowVersion;
      idempotencyKey = crypto.randomUUID();
      confirmPriceChanges = false;
      const confirmButton = wizard.querySelector('[data-confirm-price]');
      if (confirmButton) confirmButton.hidden = true;
      const notice = Array.isArray(data.notices) && data.notices.length ? ` ${data.notices[0]}` : '';
      setState(`Salvo agora.${notice}`, 'saved');
      return true;
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
  const addItem = (description = '', service = null) => { const row = rows()[0].cloneNode(true); row.querySelectorAll('input').forEach(x => x.value = x.matches('[data-item-quantity]') ? '1' : ''); delete row.dataset.serviceId; row.querySelector('[data-item-description]').value = description; if (service) { row.dataset.serviceId = service.id; row.querySelector('[data-item-price]').value = service.price; row.querySelector('[data-item-unit]').value = service.unit; } itemHost.append(row); rename(); update(); scheduleSave(); };
  const selectClient = button => {
    wizard.querySelector('[data-client-id]').value = button.dataset.clientId;
    wizard.querySelector('[data-client-name]').value = button.dataset.clientName;
    wizard.querySelectorAll('[data-client-option]').forEach(x => x.classList.toggle('is-selected', x === button));
    update(); scheduleSave();
  };
  const addClientChoice = (client, shouldSelect = true) => {
    const button = document.createElement('button');
    const detail = client.detail || `${client.city || 'Cidade não informada'} · ${client.phone || client.email || 'Contato não informado'}`;
    button.type = 'button'; button.dataset.clientOption = 'true'; button.dataset.clientId = client.id; button.dataset.clientName = client.name;
    button.dataset.clientDetail = detail; button.dataset.search = `${client.name || ''} ${client.document || ''} ${client.city || ''}`.toLocaleLowerCase('pt-BR');
    button.innerHTML = `<span><strong></strong><small></small></span><span>Selecionar</span>`;
    button.querySelector('strong').textContent = client.name;
    button.querySelector('small').textContent = detail;
    button.addEventListener('click', () => selectClient(button));
    clientChoices?.prepend(button);
    if (shouldSelect) selectClient(button);
  };
  const renderClientChoices = clients => {
    clientChoices?.replaceChildren();
    if (!clients.length) {
      const empty = document.createElement('p'); empty.className = 'of-inline-empty'; empty.textContent = 'Nenhum cliente encontrado nesta conta.';
      clientChoices?.append(empty); return;
    }
    clients.slice().reverse().forEach(client => addClientChoice(client, false));
    wizard.querySelector('[data-client-id]').value = '';
    wizard.querySelector('[data-client-name]').value = '';
    wizard.querySelectorAll('[data-client-option]').forEach(x => x.classList.remove('is-selected'));
    update();
  };
  next.addEventListener('click', () => { if (valid()) go(currentStep + 1); }); previous.addEventListener('click', () => go(currentStep - 1));
  steps.forEach(x => x.addEventListener('click', () => { const target = Number(x.dataset.stepTarget); if (target <= currentStep || valid()) go(target); }));
  wizard.querySelector('[data-item-add]').addEventListener('click', () => addItem()); wizard.querySelectorAll('[data-service-example]').forEach(x => x.addEventListener('click', () => addItem(x.dataset.serviceExample)));
  itemHost.addEventListener('click', event => {
    const row = event.target.closest('[data-budget-item]'); if (!row) return;
    if (event.target.closest('[data-item-remove]')) {
      if (rows().length === 1) { delete row.dataset.serviceId; row.querySelectorAll('input').forEach(x => { x.value = x.matches('[data-item-quantity]') ? '1' : ''; x.setCustomValidity?.(''); }); }
      else row.remove();
    }
    if (event.target.closest('[data-item-duplicate]')) {
      const clone = row.cloneNode(true); clone.querySelectorAll('input').forEach(x => x.setCustomValidity?.('')); row.after(clone);
    }
    rename(); update(); scheduleSave();
  });
  wizard.querySelectorAll('[data-client-option]').forEach(option => option.addEventListener('click', () => selectClient(option)));
  const clientSearch = wizard.querySelector('[data-client-search]');
  let clientTimer;
  clientSearch.addEventListener('input', event => {
    event.target.setCustomValidity('');
    clearTimeout(clientTimer);
    const term = event.target.value.trim();
    const seq = ++clientSearchSeq;
    clientTimer = setTimeout(async () => {
      clientSearchController?.abort();
      clientSearchController = new AbortController();
      clientChoices?.setAttribute('aria-busy', 'true');
      try {
        const response = await fetch(`?handler=ClientSearch&q=${encodeURIComponent(term)}&pageSize=12`, { signal: clientSearchController.signal });
        const data = response.ok ? await response.json() : { results: [] };
        if (seq === clientSearchSeq) renderClientChoices(data.results || []);
      } catch (error) {
        if (error.name !== 'AbortError' && seq === clientSearchSeq) {
          clientChoices?.replaceChildren();
          const failure = document.createElement('p'); failure.className = 'of-form-error'; failure.textContent = 'Não foi possível buscar clientes agora.';
          clientChoices?.append(failure);
        }
      } finally {
        if (seq === clientSearchSeq) clientChoices?.setAttribute('aria-busy', 'false');
      }
    }, 250);
  });
  wizard.querySelector('[data-quick-client-submit]')?.addEventListener('click', async event => {
    const feedback = wizard.querySelector('[data-quick-client-feedback]');
    const root = wizard.querySelector('[data-quick-client]');
    const name = wizard.querySelector('[data-quick-client-name]');
    const allowDuplicate = wizard.querySelector('[data-quick-client-allow-duplicate]');
    if (!name.value.trim()) { name.setCustomValidity('Informe o nome do cliente.'); name.reportValidity(); return; }
    event.currentTarget.disabled = true; feedback.textContent = 'Cadastrando cliente...';
    try {
      const response = await fetch('?handler=QuickClient', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
        body: JSON.stringify({
          name: name.value, phone: wizard.querySelector('[data-quick-client-phone]')?.value || null,
          email: wizard.querySelector('[data-quick-client-email]')?.value || null,
          city: wizard.querySelector('[data-quick-client-city]')?.value || null,
          documentNumber: wizard.querySelector('[data-quick-client-document]')?.value || null,
          allowPossibleDuplicate: allowDuplicate?.checked === true
        })
      });
      const data = await response.json();
      if (!response.ok) {
        feedback.replaceChildren(document.createTextNode(data.error || 'Não foi possível cadastrar.'));
        if (Array.isArray(data.candidates) && data.candidates.length) {
          const list = document.createElement('div');
          data.candidates.forEach(candidate => {
            const button = document.createElement('button'); button.type = 'button'; button.className = 'of-command-link';
            button.textContent = `Usar ${candidate.name} (${candidate.reason})`;
            button.addEventListener('click', () => addClientChoice({ id: candidate.id, name: candidate.name, detail: candidate.reason }));
            list.append(button);
          });
          feedback.append(list);
        }
        if (response.status === 422) {
          allowDuplicate.closest('label').hidden = false;
          if (!allowDuplicate.checked) feedback.append(document.createTextNode(' Marque a confirmação se realmente deseja criar outro cadastro.'));
          return;
        }
        throw new Error(data.error || 'Não foi possível cadastrar.');
      }
      if (allowDuplicate) { allowDuplicate.checked = false; allowDuplicate.closest('label').hidden = true; }
      feedback.textContent = 'Cliente cadastrado e selecionado.';
      addClientChoice(data.client);
      root.open = false;
    } catch (error) { feedback.textContent = error.message || 'Falha ao cadastrar cliente.'; }
    finally { event.currentTarget.disabled = false; }
  });
  wizard.querySelectorAll('[data-quick-client] input').forEach(input => input.addEventListener('input', () => {
    const allow = wizard.querySelector('[data-quick-client-allow-duplicate]');
    if (allow) { allow.checked = false; allow.closest('label').hidden = true; }
    wizard.querySelector('[data-quick-client-feedback]').textContent = '';
  }));
  wizard.addEventListener('input', event => { event.target.setCustomValidity?.(''); update(); scheduleSave(); }); wizard.addEventListener('change', scheduleSave);
  wizard.querySelector('[data-summary-toggle]').addEventListener('click', event => { const content = wizard.querySelector('[data-summary-content]'); content.hidden = !content.hidden; event.currentTarget.setAttribute('aria-expanded', String(!content.hidden)); });
  wizard.querySelector('[data-preview-link]').addEventListener('click', async event => { event.preventDefault(); if (await save()) window.location.assign(event.currentTarget.href); });
  finish.addEventListener('click', async () => { if (valid()) await save(true); });
  wizard.querySelectorAll('[data-initial-unit]').forEach(x => { x.value = x.dataset.initialUnit || 'serviço'; });
  wizard.querySelectorAll('[data-initial-value]').forEach(x => { if (x.dataset.initialValue) x.value = x.dataset.initialValue; });

  const serviceSearch = wizard.querySelector('[data-service-search]');
  const serviceResults = wizard.querySelector('[data-service-results]');
  let serviceTimer;
  const searchServices = () => {
    clearTimeout(serviceTimer);
    serviceTimer = setTimeout(async () => {
      const query = serviceSearch.value.trim();
      const seq = ++serviceSearchSeq;
      serviceSearchController?.abort();
      serviceSearchController = new AbortController();
      serviceResults.setAttribute('aria-busy', 'true');
      try {
        const response = await fetch(`?handler=BudgetCatalog&q=${encodeURIComponent(query)}&limit=8`, { signal: serviceSearchController.signal });
        const data = response.ok ? await response.json() : { results: [] };
        if (seq !== serviceSearchSeq) return;
        const results = data.results || [];
        if (!results.length) { serviceResults.replaceChildren(Object.assign(document.createElement('p'), { className: 'of-inline-empty', textContent: 'Nenhum serviço encontrado.' })); return; }
        serviceResults.replaceChildren(...results.map((service, index) => {
          const button = document.createElement('button'); button.type = 'button'; button.className = 'of-service-result';
          if (index === 0) button.dataset.firstServiceResult = 'true';
          const marker = service.favorite ? 'Favorito · ' : service.recurring ? 'Recorrente · ' : '';
          button.textContent = `${marker}${service.name} · ${service.unit} · ${money.format(service.price)}`;
          button.title = service.deliveryTerm || service.defaultNotes || 'Adicionar ao orçamento';
          button.addEventListener('click', () => addItem(service.description || service.name, service)); return button;
        }));
      } catch (error) {
        if (error.name !== 'AbortError' && seq === serviceSearchSeq) serviceResults.replaceChildren(Object.assign(document.createElement('p'), { className: 'of-form-error', textContent: 'Não foi possível buscar o catálogo agora.' }));
      } finally {
        if (seq === serviceSearchSeq) serviceResults.setAttribute('aria-busy', 'false');
      }
    }, 250);
  };
  serviceSearch?.addEventListener('input', searchServices);
  serviceSearch?.addEventListener('focus', searchServices);
  serviceSearch?.addEventListener('keydown', event => { if (event.key === 'ArrowDown') { event.preventDefault(); serviceResults.querySelector('[data-first-service-result]')?.focus(); } });
  rename(); update(); go(currentStep);
}
