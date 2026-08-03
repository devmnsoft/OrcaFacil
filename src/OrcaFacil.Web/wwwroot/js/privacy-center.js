const requestForm = document.querySelector('form[action*="Request"]');
requestForm?.addEventListener('submit', event => {
  const description = requestForm.querySelector('textarea');
  if (!description?.value.trim()) { event.preventDefault(); description?.focus(); }
});
