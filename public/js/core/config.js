export const APP_CONFIG = {
  name: 'OrçaFácil',
  version: '1.0.0',
  defaultPort: 8095,
  company: { name: 'MNSOFT', cnpj: '18.160.057/0001-13', email: 'comercial@mnsoft.com.br' },
  support: { whatsappNumber: '5591981809035', email: 'comercial@mnsoft.com.br' },
  firebase: { projectId: 'orcafacil-b771c' }
};
export const APP_NAME = APP_CONFIG.name;
export const APP_VERSION = APP_CONFIG.version;
export const DEFAULT_LOCAL_PORT = APP_CONFIG.defaultPort;
export const DEFAULT_PORT = DEFAULT_LOCAL_PORT;
export const SUPPORTED_PROTOCOLS = ['http:', 'https:'];
export const FIREBASE_PROJECT_ID = APP_CONFIG.firebase.projectId;
export const WHATSAPP_MNSOFT = APP_CONFIG.support.whatsappNumber;
const hasWindow = typeof window !== 'undefined';
const locationRef = hasWindow ? window.location : null;
export const IS_FILE_PROTOCOL = Boolean(locationRef && locationRef.protocol === 'file:');
export const IS_LOCALHOST = Boolean(locationRef && ['localhost', '127.0.0.1', '::1'].includes(locationRef.hostname));
export const IS_STATIC_HOSTING = Boolean(locationRef && SUPPORTED_PROTOCOLS.includes(locationRef.protocol) && !IS_LOCALHOST);
export function detectBasePath(pathname = locationRef?.pathname || '/') { return (pathname.replace(/\/[^/]*$/, '/') || '/'); }
export function detectPublicBasePath(pathname = locationRef?.pathname || '/') { return detectBasePath(pathname); }
export const BASE_PATH = hasWindow ? detectBasePath() : '/';
export const PUBLIC_BASE_PATH = BASE_PATH;
export const IS_STATIC_MODE = IS_STATIC_HOSTING;
