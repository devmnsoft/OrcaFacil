document.querySelectorAll('[data-picker-search]').forEach(input => {
  const list = document.querySelector(`[data-picker-list="${input.dataset.pickerSearch}"]`);
  if (!list) return;
  input.addEventListener('input', () => {
    const query = input.value.trim().toLocaleLowerCase('pt-BR');
    list.querySelectorAll('[data-search]').forEach(item => { item.hidden = query.length > 0 && !item.dataset.search.includes(query); });
  });
});
