export function navigateToHash(hash) { window.location.hash = hash; }
export function currentHash() { return window.location.hash || '#inicio'; }
