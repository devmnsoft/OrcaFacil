const defaults = { success: 4200, information: 5200, warning: 9000, danger: 0, progress: 0 };
const labels = { success: 'Concluído', information: 'Informação', warning: 'Atenção', danger: 'Não foi possível concluir', progress: 'Em andamento' };

class ToastManager {
  constructor() { this.host = null; this.items = new Map(); this.initialized = false; }
  init() {
    if (this.initialized) return;
    this.initialized = true;
    this.host = document.querySelector('[data-toast-host]');
    this.host?.addEventListener('click', event => { const close = event.target.closest('[data-toast-close]'); if (close) this.dismiss(close.closest('[data-toast]')); });
    this.host?.querySelectorAll('[data-toast]').forEach(toast => this.activate(toast));
  }
  show({ title, message, type = 'information', duration, id, correlationId } = {}) {
    this.init();
    if (!this.host || !message) return null;
    const key = id || `${type}:${message}`;
    if (this.items.has(key)) return this.update(key, { title, message, type });
    const toast = document.createElement('div');
    toast.className = `of-toast of-toast--${type}`; toast.dataset.toast = ''; toast.dataset.toastKey = key;
    toast.setAttribute('role', type === 'danger' ? 'alert' : 'status');
    const content = document.createElement('div'); content.className = 'of-toast__content';
    const heading = document.createElement('strong'); heading.textContent = title || labels[type] || labels.information;
    const copy = document.createElement('p'); copy.textContent = message;
    content.append(heading, copy); toast.append(content);
    if (correlationId) { const code = document.createElement('button'); code.type = 'button'; code.className = 'of-toast__correlation'; code.textContent = `Copiar código ${correlationId}`; code.onclick = () => navigator.clipboard.writeText(correlationId); toast.append(code); }
    const close = document.createElement('button'); close.type = 'button'; close.className = 'of-toast__close'; close.dataset.toastClose = ''; close.setAttribute('aria-label', 'Fechar notificação'); close.textContent = '×'; toast.append(close);
    this.host.append(toast); this.items.set(key, toast); this.activate(toast, duration ?? defaults[type]); return key;
  }
  update(key, data) { const toast = this.items.get(key); if (!toast) return null; toast.querySelector('strong').textContent = data.title || labels[data.type] || labels.information; toast.querySelector('p').textContent = data.message; return key; }
  activate(toast, duration) { requestAnimationFrame(() => toast.classList.add('is-visible')); const delay = duration ?? defaults[toast.dataset.toastType || 'information']; if (delay > 0) toast._timer = setTimeout(() => this.dismiss(toast), delay); toast.addEventListener('mouseenter', () => clearTimeout(toast._timer), { once: true }); }
  dismiss(toast) { if (!toast) return; clearTimeout(toast._timer); toast.classList.remove('is-visible'); if (toast.dataset.toastKey) this.items.delete(toast.dataset.toastKey); setTimeout(() => toast.remove(), 180); }
}

export const toastManager = new ToastManager();
toastManager.init();
window.OrcaToast = toastManager;
