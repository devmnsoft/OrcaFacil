const dialogSelector = '[role="dialog"][aria-modal="true"]';

export function safeInitDialogs(root = document) {
  root.querySelectorAll(dialogSelector).forEach(dialog => {
    if (dialog.dataset.dialogV59Ready === 'true') return;
    dialog.dataset.dialogV59Ready = 'true';
    dialog.addEventListener('keydown', event => {
      if (event.key !== 'Tab') return;
      const focusable = [...dialog.querySelectorAll('button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])')];
      if (!focusable.length) return;
      const first = focusable[0];
      const last = focusable.at(-1);
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    });
  });
}

if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', () => safeInitDialogs(), { once: true });
else safeInitDialogs();
