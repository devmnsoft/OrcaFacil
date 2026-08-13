import { overlayManager } from './overlay-manager.js';
import { toastManager } from './toast-manager.js';

let pendingConfirmation = null;
document.addEventListener('click', event => {
  const trigger = event.target.closest('[data-confirm]');
  if (!trigger || trigger.dataset.confirmed === 'true') return;
  event.preventDefault();
  const dialog = document.querySelector('#confirm-dialog');
  if (!dialog) return;
  dialog.querySelector('[data-confirm-message]').textContent = trigger.dataset.confirm || 'Confirma esta ação?';
  const impact = dialog.querySelector('[data-confirm-impact]');
  impact.textContent = trigger.dataset.confirmImpact || '';
  impact.hidden = !impact.textContent;
  pendingConfirmation = trigger;
  overlayManager.open(dialog, trigger);
});

document.querySelector('[data-confirm-accept]')?.addEventListener('click', () => {
  const trigger = pendingConfirmation;
  pendingConfirmation = null;
  overlayManager.close(document.querySelector('#confirm-dialog'), 'confirm');
  if (!trigger) return;
  trigger.dataset.confirmed = 'true';
  if (trigger instanceof HTMLAnchorElement) location.assign(trigger.href);
  else if (trigger.form) trigger.form.requestSubmit(trigger);
  else trigger.click();
});

window.showToast = (message, type = 'success') => toastManager.show({ message, type });
window.confirmAction = message => new Promise(resolve => {
  const dialog = document.querySelector('#confirm-dialog');
  if (!dialog) { resolve(window.confirm(message)); return; }
  const messageNode = dialog.querySelector('[data-confirm-message]');
  if (messageNode) messageNode.textContent = message;
  dialog.addEventListener('overlay:close', event => resolve(event.detail.result === 'confirm'), { once: true });
  const accept = dialog.querySelector('[data-confirm-accept]');
  accept?.addEventListener('click', () => overlayManager.close(dialog, 'confirm'), { once: true });
  overlayManager.open(dialog);
});
