document.querySelectorAll('[data-picker-search]').forEach(input => {
  const list = document.querySelector(`[data-picker-list="${input.dataset.pickerSearch}"]`);
  if (!list) return;
  input.addEventListener('input', () => {
    const query = input.value.trim().toLocaleLowerCase('pt-BR');
    list.querySelectorAll('[data-search]').forEach(item => { item.hidden = query.length > 0 && !item.dataset.search.includes(query); });
  });
});

document.querySelectorAll('[data-service-picker]').forEach(form => {
  const submit = form.querySelector('[data-service-submit]');
  form.addEventListener('change', () => {
    const count = form.querySelectorAll('input[name="serviceIds"]:checked').length;
    submit.disabled = count === 0;
    submit.textContent = count ? `Adicionar ${count} serviço${count > 1 ? 's' : ''}` : 'Adicionar serviços selecionados';
  });
  form.addEventListener('submit', () => { submit.disabled = true; submit.setAttribute('aria-busy', 'true'); submit.textContent = 'Preparando orçamento…'; });
});
