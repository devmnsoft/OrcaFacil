const widget = document.querySelector('[data-feedback-widget]');
if (widget) {
  const panel = widget.querySelector('[data-feedback-panel]');
  widget.querySelector('[data-feedback-open]')?.addEventListener('click', () => { panel.hidden = false; panel.querySelector('input[type="radio"]')?.focus(); });
  widget.querySelector('[data-feedback-close]')?.addEventListener('click', () => { panel.hidden = true; });
  widget.querySelector('form')?.addEventListener('submit', () => { const button=widget.querySelector('button[type="submit"]'); if(button){button.disabled=true;button.textContent='Enviando…';} });
}
