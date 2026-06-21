export function isFileProtocol(locationRef = window.location) { return locationRef.protocol === 'file:'; }
export function isLocalhost(locationRef = window.location) { return ['localhost', '127.0.0.1', '::1'].includes(locationRef.hostname); }
export function isFirebaseHosting(locationRef = window.location) { return /(?:web\.app|firebaseapp\.com)$/.test(locationRef.hostname); }
export function isIISLike(locationRef = window.location) { return !isFileProtocol(locationRef) && !isFirebaseHosting(locationRef) && !isLocalhost(locationRef); }
export function getBasePath(locationRef = window.location) { return (locationRef.pathname || '/').replace(/\/[^/]*$/, '/') || '/'; }
export function getPublicUrl(path = '', locationRef = window.location) { const clean = String(path).replace(/^\/+/, ''); return new URL(clean, locationRef.origin + getBasePath(locationRef)).toString(); }
export function detectEnvironment(locationRef = window.location) {
  const file = isFileProtocol(locationRef);
  const localhost = !file && isLocalhost(locationRef);
  const firebase = !file && isFirebaseHosting(locationRef);
  const iis = isIISLike(locationRef);
  return { protocol: locationRef.protocol, host: locationRef.host, hostname: locationRef.hostname, href: locationRef.href, isFile: file, isLocalhost: localhost, isFirebaseHosting: firebase, isIISLike: iis, basePath: getBasePath(locationRef), mode: file ? 'file' : localhost ? 'local' : firebase ? 'firebase-hosting' : iis ? 'iis-static' : 'static' };
}
