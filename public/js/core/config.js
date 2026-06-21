export const APP_NAME = 'OrçaFácil';
export const APP_VERSION = '1.0.0';
export const DEFAULT_PORT = 8095;
export const FIREBASE_PROJECT_ID = 'orcafacil-b771c';
export const WHATSAPP_MNSOFT = '5591981809035';
export const IS_STATIC_MODE = typeof window !== 'undefined' && !['localhost', '127.0.0.1'].includes(window.location.hostname);

export function detectPublicBasePath(pathname = window.location.pathname) {
  const clean = pathname.replace(/\/[^/]*$/, '/');
  return clean.endsWith('/public/') ? clean : clean;
}

export const PUBLIC_BASE_PATH = typeof window !== 'undefined' ? detectPublicBasePath() : '/';
