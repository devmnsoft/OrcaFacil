const focusableSelector = 'a[href],button:not([disabled]),input:not([disabled]),select:not([disabled]),textarea:not([disabled]),[tabindex]:not([tabindex="-1"])';

class OverlayManager {
  constructor() { this.stack = []; this.initialized = false; }
  init() {
    if (this.initialized) return;
    this.initialized = true;
    document.addEventListener('keydown', event => this.onKeydown(event));
    document.addEventListener('click', event => {
      const close = event.target.closest('[data-overlay-close]');
      if (close) this.close(close.closest('[data-overlay]'));
      const overlay = event.target.matches?.('[data-overlay]') ? event.target : null;
      if (overlay?.dataset.backdropClose === 'true') this.close(overlay);
    });
  }
  open(target, trigger = document.activeElement) {
    const overlay = typeof target === 'string' ? document.querySelector(target) : target;
    if (!overlay || this.stack.some(item => item.overlay === overlay)) return false;
    overlay.hidden = false;
    overlay.setAttribute('aria-hidden', 'false');
    trigger?.setAttribute?.('aria-expanded', 'true');
    this.stack.push({ overlay, trigger });
    document.documentElement.classList.add('of-overlay-open');
    requestAnimationFrame(() => {
      overlay.classList.add('is-open');
      (overlay.querySelector('[autofocus]') || overlay.querySelector(focusableSelector) || overlay).focus({ preventScroll: true });
    });
    return true;
  }
  close(target, result = 'cancel') {
    const overlay = target || this.stack.at(-1)?.overlay;
    const index = this.stack.findIndex(item => item.overlay === overlay);
    if (index < 0 || overlay.dataset.busy === 'true') return false;
    const [{ trigger }] = this.stack.splice(index, 1);
    overlay.classList.remove('is-open');
    overlay.setAttribute('aria-hidden', 'true');
    setTimeout(() => { if (!overlay.classList.contains('is-open')) overlay.hidden = true; }, 180);
    trigger?.setAttribute?.('aria-expanded', 'false');
    trigger?.focus?.({ preventScroll: true });
    if (!this.stack.length) document.documentElement.classList.remove('of-overlay-open');
    overlay.dispatchEvent(new CustomEvent('overlay:close', { detail: { result } }));
    return true;
  }
  onKeydown(event) {
    const current = this.stack.at(-1)?.overlay;
    if (!current) return;
    if (event.key === 'Escape' && current.dataset.critical !== 'true') { event.preventDefault(); this.close(current); return; }
    if (event.key !== 'Tab') return;
    const focusables = [...current.querySelectorAll(focusableSelector)].filter(el => el.offsetParent !== null);
    if (!focusables.length) { event.preventDefault(); current.focus(); return; }
    const first = focusables[0], last = focusables.at(-1);
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
  }
}

export const overlayManager = new OverlayManager();
overlayManager.init();
window.OrcaOverlay = overlayManager;
