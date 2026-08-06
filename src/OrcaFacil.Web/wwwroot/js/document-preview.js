const zoom = document.querySelector('[data-server-preview-zoom]');
const page = document.querySelector('[data-server-preview-page]');
zoom?.addEventListener('input', () => page?.style.setProperty('--preview-scale', String(Number(zoom.value) / 100)));
