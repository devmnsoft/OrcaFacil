export const APP_NAME = 'OrçaFácil';
export const APP_VERSION = '1.0.0';
export const DEFAULT_LOCAL_PORT = 8095;
export const DEFAULT_PORT = DEFAULT_LOCAL_PORT;
export const SUPPORTED_PROTOCOLS = ['http:', 'https:'];
export const FIREBASE_PROJECT_ID = 'orcafacil-b771c';
export const WHATSAPP_MNSOFT = '5591981809035';

const hasWindow = typeof window !== 'undefined';
const locationRef = hasWindow ? window.location : null;

export const IS_FILE_PROTOCOL = Boolean(locationRef && locationRef.protocol === 'file:');
export const IS_LOCALHOST = Boolean(
  locationRef && ['localhost', '127.0.0.1', '::1'].includes(locationRef.hostname)
);
export const IS_STATIC_HOSTING = Boolean(
  locationRef && SUPPORTED_PROTOCOLS.includes(locationRef.protocol) && !IS_LOCALHOST
);

export function detectBasePath(pathname = locationRef?.pathname || '/') {
  const clean = pathname.replace(/\/[^/]*$/, '/');
  return clean || '/';
}

export function detectPublicBasePath(pathname = locationRef?.pathname || '/') {
  return detectBasePath(pathname);
}

export const BASE_PATH = hasWindow ? detectBasePath() : '/';
export const PUBLIC_BASE_PATH = BASE_PATH;
export const IS_STATIC_MODE = IS_STATIC_HOSTING;
